using System.Collections.Generic;

namespace yuna0x0.Basis.Convert.Model
{
    /// <summary>Who may wear a VRM avatar, in both formats' shared ordering.</summary>
    public enum VrmAvatarPermission
    {
        OnlyAuthor = 0,
        ExplicitlyLicensedPerson = 1,
        Everyone = 2,
    }

    /// <summary>What VRM 1.0 says about changing an avatar.</summary>
    public enum VrmModificationPermission
    {
        Prohibited = 0,
        AllowModification = 1,
        AllowModificationRedistribution = 2,
    }

    /// <summary>
    /// The licence a VRM avatar carries.
    /// <para>
    /// Every VRM states who may use it and what may be done to it. Converting one is a
    /// modification, so the licence is read and shown before anything is written. Nothing here
    /// changes what a conversion does: it is the wearer's to judge, and this only makes sure
    /// they see it.
    /// </para>
    /// </summary>
    public sealed class VrmMetaData
    {
        public string Title = string.Empty;
        public List<string> Authors = new List<string>();

        public VrmAvatarPermission AvatarPermission = VrmAvatarPermission.OnlyAuthor;

        /// <summary>VRM 1.0 only. VRM 0.x states this through its licence type instead.</summary>
        public VrmModificationPermission? Modification;

        /// <summary>The 0.x licence type, as its own enum names it.</summary>
        public string LicenseName = string.Empty;

        /// <summary>
        /// What the avatar may be used for. Null means the format did not state it: VRM 0.x has
        /// no field for the political and antisocial ones.
        /// </summary>
        public bool? ViolentUsage;

        public bool? SexualUsage;
        public bool? PoliticalOrReligiousUsage;
        public bool? AntisocialOrHateUsage;

        /// <summary>
        /// How far commercial use goes. VRM 0.x allows or disallows it; 1.0 distinguishes
        /// personal non-profit, personal profit and corporate use.
        /// </summary>
        public string CommercialUsage = string.Empty;

        /// <summary>Whether the avatar may be passed on. VRM 1.0 only.</summary>
        public bool? Redistribution;

        /// <summary>Whether the author has to be credited. VRM 1.0 only.</summary>
        public bool? CreditRequired;

        public string LicenseUrl = string.Empty;

        /// <summary>True when the avatar names a licence that forbids changing it.</summary>
        public bool ForbidsModification =>
            Modification == VrmModificationPermission.Prohibited
            || LicenseName == "CC_BY_ND" || LicenseName == "CC_BY_NC_ND";

        public bool HasAnything =>
            !string.IsNullOrEmpty(Title) || Authors.Count > 0
            || !string.IsNullOrEmpty(LicenseName) || Modification.HasValue;

        /// <summary>
        /// Every permission the avatar states, in the words its own format uses. A permission
        /// the format has no field for is left out rather than guessed at.
        /// </summary>
        public IEnumerable<string> Permissions()
        {
            yield return "Wearing: " + AvatarPermission switch
            {
                VrmAvatarPermission.Everyone => "anyone",
                VrmAvatarPermission.ExplicitlyLicensedPerson => "licensed people only",
                _ => "the author only",
            };

            if (Modification.HasValue)
            {
                yield return "Changes: " + Modification.Value switch
                {
                    VrmModificationPermission.AllowModificationRedistribution =>
                        "allowed, and may be passed on",
                    VrmModificationPermission.AllowModification => "allowed",
                    _ => "not allowed",
                };
            }

            if (!string.IsNullOrEmpty(CommercialUsage))
            {
                yield return $"Commercial use: {CommercialUsage}";
            }

            foreach (string permission in new[]
                     {
                         Permission("Violence", ViolentUsage),
                         Permission("Sexual content", SexualUsage),
                         Permission("Political or religious use", PoliticalOrReligiousUsage),
                         Permission("Antisocial or hateful use", AntisocialOrHateUsage),
                         Permission("Passing it on", Redistribution),
                     })
            {
                if (permission != null)
                {
                    yield return permission;
                }
            }

            if (CreditRequired.HasValue)
            {
                yield return "Credit: " + (CreditRequired.Value ? "required" : "not required");
            }

            if (!string.IsNullOrEmpty(LicenseName))
            {
                yield return $"Licence: {LicenseName}";
            }
        }

        private static string Permission(string name, bool? allowed) =>
            allowed.HasValue ? $"{name}: {(allowed.Value ? "allowed" : "not allowed")}" : null;

        /// <summary>Who made it, which is the line above the permissions.</summary>
        public string Describe()
        {
            string author = Authors.Count > 0 ? string.Join(", ", Authors) : "an unnamed author";
            string title = string.IsNullOrEmpty(Title) ? "This VRM avatar" : $"'{Title}'";

            // Studio names commonly end in a full stop of their own, as "VirtualCast, Inc." does.
            return $"{title} by {author.TrimEnd('.')}.";
        }

        /// <summary>The whole licence on one line, for the report.</summary>
        public string Summarise() =>
            Describe() + " " + string.Join(". ", Permissions()) + ".";
    }
}
