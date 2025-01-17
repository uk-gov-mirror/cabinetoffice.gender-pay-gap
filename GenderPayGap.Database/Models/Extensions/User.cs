using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using GenderPayGap.Core;
using GenderPayGap.Extensions;

namespace GenderPayGap.Database
{

    [Serializable]
    [DebuggerDisplay("{UserId}, {EmailAddress}, {Status}")]
    public partial class User
    {

        [NotMapped]
        public string EmailAddress
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(EmailAddressDB))
                {
                    try
                    {
                        return Encryption.DecryptData(EmailAddressDB);
                    }
                    catch (CryptographicException) { }
                }

                return EmailAddressDB;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    EmailAddressDB = Encryption.EncryptData(value.ToLower());
                }
                else
                {
                    EmailAddressDB = value;
                }
            }
        }

        [NotMapped]
        public string ContactEmailAddress
        {
            get => string.IsNullOrWhiteSpace(ContactEmailAddressDB) ? ContactEmailAddressDB : Encryption.DecryptData(ContactEmailAddressDB);
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    ContactEmailAddressDB = Encryption.EncryptData(value);
                }
                else
                {
                    ContactEmailAddressDB = value;
                }
            }
        }

        [NotMapped]
        public string Fullname => (Firstname + " " + Lastname).TrimI();

        [NotMapped]
        public string ContactFullname => (ContactFirstName + " " + ContactLastName).TrimI();

        public bool IsAdministrator()
        {
            if (!EmailAddress.IsEmailAddress())
            {
                throw new ArgumentException("Bad email address");
            }

            if (string.IsNullOrWhiteSpace(Global.AdminEmails))
            {
                throw new ArgumentException("Missing AdminEmails from web.config");
            }

            return EmailAddress.LikeAny(Global.AdminEmails.SplitI(";"));
        }

        /// <summary>
        ///     Determines if the user is the only registration of any of their UserOrganisations
        /// </summary>
        public bool IsSoleUserOfOneOrMoreOrganisations()
        {
            return UserOrganisations.Any(uo => uo.GetAssociatedUsers().Any() == false);
        }

        public void SetStatus(UserStatuses status, User byUser, string details = null)
        {
            //ByUser must be an object and not the id itself otherwise a foreign key exception is thrown with EF core due to being unable to resolve the ByUserId
            if (status == Status && details == StatusDetails)
            {
                return;
            }

            UserStatuses.Add(
                new UserStatus {
                    User = this,
                    Status = status,
                    StatusDate = VirtualDateTime.Now,
                    StatusDetails = details,
                    ByUser = byUser
                });
            Status = status;
            StatusDate = VirtualDateTime.Now;
            StatusDetails = details;
        }

        public override bool Equals(object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var target = (User) obj;
            return UserId == target.UserId;
        }

    }

}
