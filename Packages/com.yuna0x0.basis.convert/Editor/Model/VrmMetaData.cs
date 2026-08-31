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

        public string LicenseUrl = string.Empty;

        /// <summary>True when the avatar names a licence that forbids changing it.</summary>
        public bool ForbidsModification =>
            Modification == VrmModificationPermission.Prohibited
            || LicenseName == "CC_BY_ND" || LicenseName == "CC_BY_NC_ND";

        public bool HasAnything =>
            !string.IsNullOrEmpty(Title) || Authors.Count > 0
            || !string.IsNullOrEmpty(LicenseName) || Modification.HasValue;

        public string Describe()
        {
            string who = AvatarPermission switch
            {
                VrmAvatarPermission.Everyone => "anyone may wear it",
                VrmAvatarPermission.ExplicitlyLicensedPerson => "only licensed people may wear it",
                _ => "only its author may wear it",
            };

            string author = Authors.Count > 0 ? string.Join(", ", Authors) : "an unnamed author";
            string title = string.IsNullOrEmpty(Title) ? "This VRM avatar" : $"'{Title}'";

            return $"{title} by {author}: {who}.";
        }
    }
}
