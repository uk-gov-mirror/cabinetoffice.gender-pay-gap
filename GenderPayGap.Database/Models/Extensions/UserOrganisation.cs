using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GenderPayGap.Core;
using GenderPayGap.Extensions;

namespace GenderPayGap.Database
{
    [Serializable]
    [DebuggerDisplay("({Organisation}),({User})")]
    public partial class UserOrganisation
    {

        public string GetReviewCode()
        {
            return Encryption.EncryptQuerystring(UserId + ":" + OrganisationId + ":" + VirtualDateTime.Now.ToSmallDateTime());
        }

        public IEnumerable<UserOrganisation> GetAssociatedUsers()
        {
            return Organisation.UserOrganisations
                .Where(
                    uo => uo.OrganisationId == OrganisationId
                          && uo.UserId != UserId
                          && uo.HasBeenActivated()
                          && uo.User.Status == UserStatuses.Active);
        }

        public bool HasBeenActivated()
        {
            return PINConfirmedDate != null;
        }
        
        public bool IsAwaitingActivationPIN()
        {
            return PINSentDate.HasValue && PINConfirmedDate == null;
        }

        public bool IsAwaitingRegistrationApproval()
        {
            return PINSentDate == null && PINConfirmedDate == null;
        }

        public bool HasExpiredPin()
        {
            return IsAwaitingActivationPIN() && PINSentDate.Value.AddDays(Global.PinInPostExpiryDays) < VirtualDateTime.Now;
        }

    }
}
