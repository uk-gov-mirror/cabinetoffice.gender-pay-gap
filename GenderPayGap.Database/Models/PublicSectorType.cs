﻿using System;
using GenderPayGap.Extensions;
using Newtonsoft.Json;

namespace GenderPayGap.Database
{

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class PublicSectorType
    {

        [JsonProperty]
        public int PublicSectorTypeId { get; set; }

        [JsonProperty]
        public string Description { get; set; }

        [JsonProperty]
        public DateTime Created { get; set; } = VirtualDateTime.Now;

        public override bool Equals(object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var target = (PublicSectorType) obj;
            return PublicSectorTypeId == target.PublicSectorTypeId;
        }

    }

}
