using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;
using Object = UnityEngine.Object;

namespace yuna0x0.Basis.Convert.Sources
{
    /// <summary>
    /// Reads UniVRM's spring bones and node constraints from live components rather than from a
    /// prefab's text.
    /// <para>
    /// A `.vrm` is binary glTF behind a ScriptedImporter, so there is no YAML to scan. UniVRM has
    /// to be installed for the file to import at all, which means its components are real types
    /// on the imported hierarchy and their serialized fields can be read directly. The field
    /// names are the ones the text would have carried, so this produces the same plain data as
    /// <see cref="VrmDocumentReader"/> and everything after it is shared.
    /// </para>
    /// <para>
    /// Identifiers are each object's own local file id inside the imported asset, which is what
    /// <see cref="PrefabObjectResolver"/> indexes, so the two paths address objects the same way.
    /// </para>
    /// </summary>
    public static class VrmComponentReader
    {
        /// <summary>What one hierarchy's VRM components amount to, ready to be assembled.</summary>
        public sealed class Result
        {
            public readonly List<VrmSpringChainData> Chains = new List<VrmSpringChainData>();

            public readonly Dictionary<long, VrmSpringJointData> Joints =
                new Dictionary<long, VrmSpringJointData>();

            public readonly Dictionary<long, VrmColliderData> Colliders =
                new Dictionary<long, VrmColliderData>();

            public readonly Dictionary<long, VrmColliderGroupData> Groups =
                new Dictionary<long, VrmColliderGroupData>();

            public readonly List<VrmConstraintData> Constraints = new List<VrmConstraintData>();

            /// <summary>The Vrm10Instance, or the 0.x components, if one was found.</summary>
            public Component Instance;

            public Component Meta;
            public Component FirstPerson;
            public Component BlendShapeProxy;

            public int ComponentsRead;
            public bool Any => ComponentsRead > 0;
        }

        public static Result Read(GameObject root)
        {
            Result result = new Result();
            if (root == null)
            {
                return result;
            }

            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    continue;
                }

                SourceComponentKind kind = KindOf(component);
                if (kind == SourceComponentKind.Unknown)
                {
                    continue;
                }

                result.ComponentsRead++;
                long id = IdOf(component);
                SerializedObject serialized = new SerializedObject(component);

                switch (kind)
                {
                    case SourceComponentKind.Vrm10SpringBoneJoint:
                        result.Joints[id] = ReadJoint(component, serialized);
                        break;

                    case SourceComponentKind.Vrm10SpringBoneCollider:
                        result.Colliders[id] = ReadCollider(component, serialized, id);
                        break;

                    case SourceComponentKind.Vrm10SpringBoneColliderGroup:
                        result.Groups[id] = ReadColliderGroup10(component, serialized, id);
                        break;

                    case SourceComponentKind.VrmSpringBoneColliderGroup:
                        result.Groups[id] = ReadColliderGroup0X(component, serialized, id);
                        break;

                    case SourceComponentKind.VrmSpringBone:
                        result.Chains.AddRange(ReadSpringBone0X(component, serialized, id));
                        break;

                    case SourceComponentKind.Vrm10Instance:
                        result.Instance = component;
                        result.Chains.AddRange(ReadInstanceSprings(serialized, id));
                        break;

                    case SourceComponentKind.VrmMeta:
                        result.Meta = component;
                        break;

                    case SourceComponentKind.VrmFirstPerson:
                        result.FirstPerson = component;
                        break;

                    case SourceComponentKind.VrmBlendShapeProxy:
                        result.BlendShapeProxy = component;
                        break;

                    default:
                        if (KnownScriptIdentities.IsVrmConstraint(kind))
                        {
                            result.Constraints.Add(ReadConstraint(component, serialized, kind, id));
                        }
                        else
                        {
                            // Recognised, but not ours to read here: counted so a hierarchy that
                            // holds only these is not called empty.
                        }

                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Which component this is, by the guid of the script behind it. The same table the text
        /// path uses, so both recognise exactly the same set.
        /// </summary>
        public static SourceComponentKind KindOf(Component component)
        {
            if (!(component is MonoBehaviour behaviour))
            {
                return SourceComponentKind.Unknown;
            }

            MonoScript script = MonoScript.FromMonoBehaviour(behaviour);
            if (script == null
                || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    script, out string guid, out long fileId))
            {
                return SourceComponentKind.Unknown;
            }

            return KnownScriptIdentities.Resolve(guid, fileId);
        }

        /// <summary>
        /// The object's own identifier inside the asset it belongs to, which is the number the
        /// file would have referred to it by.
        /// </summary>
        public static long IdOf(Object live)
        {
            return live != null
                   && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(live, out string _, out long id)
                ? id
                : 0L;
        }

        private static VrmSpringJointData ReadJoint(Component component, SerializedObject serialized)
        {
            return new VrmSpringJointData
            {
                OwnerGameObjectFileId = IdOf(component.gameObject),
                Stiffness = Float(serialized, "m_stiffnessForce", 1f),
                GravityPower = Float(serialized, "m_gravityPower", 0f),
                GravityDir = Vector(serialized, "m_gravityDir", Vector3.down),
                DragForce = Float(serialized, "m_dragForce", 0.4f),
                Radius = Float(serialized, "m_jointRadius", 0.02f),
            };
        }

        private static VrmColliderData ReadCollider(
            Component component, SerializedObject serialized, long id)
        {
            return new VrmColliderData
            {
                DocumentFileId = id,
                OwnerGameObjectFileId = IdOf(component.gameObject),
                Type = (VrmColliderType)Int(serialized, "ColliderType", 0),
                Offset = Vector(serialized, "Offset", Vector3.zero),
                Radius = Float(serialized, "Radius", 0f),
                Tail = Vector(serialized, "Tail", Vector3.zero),
                Normal = Vector(serialized, "Normal", Vector3.up),
            };
        }

        private static VrmColliderGroupData ReadColliderGroup10(
            Component component, SerializedObject serialized, long id)
        {
            VrmColliderGroupData group = new VrmColliderGroupData
            {
                DocumentFileId = id,
                OwnerGameObjectFileId = IdOf(component.gameObject),
                Name = Text(serialized, "Name"),
            };

            group.ColliderFileIds.AddRange(References(serialized, "Colliders"));
            return group;
        }

        /// <summary>VRM 0.x keeps the shapes in the group itself, as an offset and a radius.</summary>
        private static VrmColliderGroupData ReadColliderGroup0X(
            Component component, SerializedObject serialized, long id)
        {
            VrmColliderGroupData group = new VrmColliderGroupData
            {
                DocumentFileId = id,
                OwnerGameObjectFileId = IdOf(component.gameObject),
            };

            SerializedProperty list = serialized.FindProperty("Colliders");
            if (list == null || !list.isArray)
            {
                return group;
            }

            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                SerializedProperty offset = entry.FindPropertyRelative("Offset");
                SerializedProperty radius = entry.FindPropertyRelative("Radius");

                group.InlineColliders.Add(new VrmColliderData
                {
                    DocumentFileId = id,
                    OwnerGameObjectFileId = group.OwnerGameObjectFileId,
                    Offset = offset == null ? Vector3.zero : offset.vector3Value,
                    Radius = radius == null ? 0f : radius.floatValue,
                });
            }

            return group;
        }

        /// <summary>
        /// A VRM 0.x spring bone: one set of parameters and a list of root bones, each of which
        /// becomes a chain of its own.
        /// </summary>
        private static List<VrmSpringChainData> ReadSpringBone0X(
            Component component, SerializedObject serialized, long id)
        {
            List<VrmSpringChainData> chains = new List<VrmSpringChainData>();

            VrmSpringJointData joint = new VrmSpringJointData
            {
                OwnerGameObjectFileId = IdOf(component.gameObject),
                Stiffness = Float(serialized, "m_stiffnessForce", 1f),
                GravityPower = Float(serialized, "m_gravityPower", 0f),
                GravityDir = Vector(serialized, "m_gravityDir", Vector3.down),
                DragForce = Float(serialized, "m_dragForce", 0.4f),
                Radius = Float(serialized, "m_hitRadius", 0.02f),
            };

            string name = Text(serialized, "m_comment");
            List<long> colliderGroups = References(serialized, "ColliderGroups");
            long center = Reference(serialized, "m_center");

            foreach (long root in References(serialized, "RootBones"))
            {
                if (root == 0L)
                {
                    continue;
                }

                VrmSpringChainData chain = new VrmSpringChainData
                {
                    Name = name,
                    DocumentFileId = id,
                    RootTransformFileId = root,
                    CenterFileId = center,
                };

                chain.ColliderGroupFileIds.AddRange(colliderGroups);
                chain.Joints.Add(joint);
                chains.Add(chain);
            }

            return chains;
        }

        /// <summary>
        /// The chains a VRM 1.0 avatar declares. The instance names each spring's joints in
        /// order; the joints themselves are components elsewhere in the hierarchy.
        /// </summary>
        private static List<VrmSpringChainData> ReadInstanceSprings(
            SerializedObject serialized, long id)
        {
            List<VrmSpringChainData> chains = new List<VrmSpringChainData>();
            SerializedProperty springs = serialized.FindProperty("SpringBone.Springs");

            if (springs == null || !springs.isArray)
            {
                return chains;
            }

            for (int i = 0; i < springs.arraySize; i++)
            {
                SerializedProperty spring = springs.GetArrayElementAtIndex(i);
                SerializedProperty name = spring.FindPropertyRelative("Name");
                SerializedProperty center = spring.FindPropertyRelative("Center");

                VrmSpringChainData chain = new VrmSpringChainData
                {
                    DocumentFileId = id,
                    IsVrm10 = true,
                    Name = name == null ? string.Empty : name.stringValue,
                    CenterFileId = center == null ? 0L : IdOf(center.objectReferenceValue),
                };

                chain.JointComponentFileIds.AddRange(
                    References(spring.FindPropertyRelative("Joints")));
                chain.ColliderGroupFileIds.AddRange(
                    References(spring.FindPropertyRelative("ColliderGroups")));

                chains.Add(chain);
            }

            return chains;
        }

        private static VrmConstraintData ReadConstraint(
            Component component, SerializedObject serialized, SourceComponentKind kind, long id)
        {
            VrmConstraintKind constraintKind = kind switch
            {
                SourceComponentKind.Vrm10AimConstraint => VrmConstraintKind.Aim,
                SourceComponentKind.Vrm10RollConstraint => VrmConstraintKind.Roll,
                _ => VrmConstraintKind.Rotation,
            };

            VrmConstraintData constraint = new VrmConstraintData
            {
                DocumentFileId = id,
                Kind = constraintKind,
                OwnerGameObjectFileId = IdOf(component.gameObject),
                SourceTransformFileId = Reference(serialized, "Source"),
                Weight = Float(serialized, "Weight", 1f),
            };

            if (constraintKind == VrmConstraintKind.Aim)
            {
                constraint.AimAxis = (VrmAimAxis)Int(serialized, "AimAxis", 0);
            }

            if (constraintKind == VrmConstraintKind.Roll)
            {
                constraint.RollAxis = Int(serialized, "RollAxis", 0);
            }

            return constraint;
        }


        /// <summary>
        /// A VRM 1.0 avatar's look at and first person settings, held in the object asset its
        /// instance points at. In an imported `.vrm` that asset is a sub-asset of the binary
        /// file, so it is read through the object API rather than followed off disk as text.
        /// </summary>
        public static VrmAvatarSettingsData ReadSettings10(Component instance)
        {
            VrmAvatarSettingsData settings = new VrmAvatarSettingsData();
            SerializedObject vrm = ObjectAsset(instance, "Vrm");

            if (vrm == null)
            {
                return settings;
            }

            SerializedProperty offset = vrm.FindProperty("LookAt.OffsetFromHead");
            if (offset != null)
            {
                settings.EyeOffsetFromHead = offset.vector3Value;
                settings.HasEyeOffset = offset.vector3Value != Vector3.zero;
            }

            CountFirstPersonFlags(vrm.FindProperty("FirstPerson.Renderers"), settings);
            return settings;
        }

        /// <summary>A VRM 0.x avatar's first person settings, which is where its eye offset lives.</summary>
        public static VrmAvatarSettingsData ReadSettings0X(Component firstPerson)
        {
            VrmAvatarSettingsData settings = new VrmAvatarSettingsData();
            if (firstPerson == null)
            {
                return settings;
            }

            SerializedObject serialized = new SerializedObject(firstPerson);
            settings.HeadBoneFileId = Reference(serialized, "FirstPersonBone");

            SerializedProperty offset = serialized.FindProperty("FirstPersonOffset");
            if (offset != null)
            {
                settings.EyeOffsetFromHead = offset.vector3Value;
                settings.HasEyeOffset = offset.vector3Value != Vector3.zero;
            }

            CountFirstPersonFlags(serialized.FindProperty("Renderers"), settings);
            return settings;
        }

        /// <summary>
        /// Counts the renderers hidden from one view or the other. Both formats write the same
        /// four values in the same order: auto, both, third person only, first person only.
        /// </summary>
        private static void CountFirstPersonFlags(
            SerializedProperty renderers, VrmAvatarSettingsData settings)
        {
            if (renderers == null || !renderers.isArray)
            {
                return;
            }

            for (int i = 0; i < renderers.arraySize; i++)
            {
                SerializedProperty flag = renderers.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("FirstPersonFlag");

                if (flag == null)
                {
                    continue;
                }

                if (flag.intValue == 2)
                {
                    settings.ThirdPersonOnlyRenderers++;
                }
                else if (flag.intValue == 3)
                {
                    settings.FirstPersonOnlyRenderers++;
                }
            }
        }

        /// <summary>The licence a VRM 1.0 avatar carries.</summary>
        public static VrmMetaData ReadMeta10(Component instance)
        {
            SerializedObject vrm = ObjectAsset(instance, "Vrm");
            SerializedProperty meta = vrm?.FindProperty("Meta");

            if (meta == null)
            {
                return null;
            }

            VrmMetaData data = new VrmMetaData
            {
                Title = Relative(meta, "Name")?.stringValue ?? string.Empty,
                LicenseUrl = Relative(meta, "OtherLicenseUrl")?.stringValue ?? string.Empty,
            };

            SerializedProperty authors = Relative(meta, "Authors");
            if (authors != null && authors.isArray)
            {
                for (int i = 0; i < authors.arraySize; i++)
                {
                    string name = authors.GetArrayElementAtIndex(i).stringValue;
                    if (!string.IsNullOrEmpty(name))
                    {
                        data.Authors.Add(name);
                    }
                }
            }

            if (Relative(meta, "AvatarPermission") is SerializedProperty who)
            {
                data.AvatarPermission = (VrmAvatarPermission)who.intValue;
            }

            if (Relative(meta, "Modification") is SerializedProperty change)
            {
                data.Modification = (VrmModificationPermission)change.intValue;
            }

            data.ViolentUsage = Flag(meta, "ViolentUsage");
            data.SexualUsage = Flag(meta, "SexualUsage");
            data.PoliticalOrReligiousUsage = Flag(meta, "PoliticalOrReligiousUsage");
            data.AntisocialOrHateUsage = Flag(meta, "AntisocialOrHateUsage");
            data.Redistribution = Flag(meta, "Redistribution");

            // required, unnecessary
            if (Relative(meta, "CreditNotation") is SerializedProperty credit)
            {
                data.CreditRequired = credit.intValue == 0;
            }

            if (Relative(meta, "CommercialUsage") is SerializedProperty commercial)
            {
                data.CommercialUsage = Vrm10CommercialNames[
                    Mathf.Clamp(commercial.intValue, 0, Vrm10CommercialNames.Length - 1)];
            }

            return data;
        }

        /// <summary>The licence a VRM 0.x avatar carries, from the asset its meta component names.</summary>
        public static VrmMetaData ReadMeta0X(Component meta)
        {
            SerializedObject asset = ObjectAsset(meta, "Meta");
            if (asset == null)
            {
                return null;
            }

            VrmMetaData data = new VrmMetaData
            {
                Title = Text(asset, "Title"),
                LicenseUrl = Text(asset, "OtherLicenseUrl"),
            };

            string author = Text(asset, "Author");
            if (!string.IsNullOrEmpty(author))
            {
                data.Authors.Add(author);
            }

            if (asset.FindProperty("AllowedUser") is SerializedProperty allowed)
            {
                data.AvatarPermission = (VrmAvatarPermission)allowed.intValue;
            }

            if (asset.FindProperty("LicenseType") is SerializedProperty license)
            {
                data.LicenseName = Vrm0LicenseNames[
                    Mathf.Clamp(license.intValue, 0, Vrm0LicenseNames.Length - 1)];
            }

            // VRM 0.x writes each of these as Disallow or Allow, and has no field for the
            // political or antisocial ones that 1.0 added. The spelling is UniVRM's own.
            data.ViolentUsage = Allowed(asset, "ViolentUssage");
            data.SexualUsage = Allowed(asset, "SexualUssage");

            if (Allowed(asset, "CommercialUssage") is bool commercial)
            {
                data.CommercialUsage = commercial ? "allowed" : "not allowed";
            }

            return data;
        }

        /// <summary>
        /// The expressions a VRM 1.0 avatar declares. The presets are named fields and the rest
        /// are a list, and both reference expression assets beside the object asset.
        /// </summary>
        public static List<VrmExpressionData> ReadExpressions10(Component instance)
        {
            List<VrmExpressionData> expressions = new List<VrmExpressionData>();
            SerializedObject vrm = ObjectAsset(instance, "Vrm");

            if (vrm == null)
            {
                return expressions;
            }

            foreach ((string Field, VrmExpressionRole Role) preset in Vrm10Presets)
            {
                SerializedProperty clip = vrm.FindProperty("Expression." + preset.Field);
                if (clip?.objectReferenceValue == null)
                {
                    continue;
                }

                VrmExpressionData expression = ReadClip10(clip.objectReferenceValue);
                expression.Name = preset.Field;
                expression.Role = preset.Role;
                expressions.Add(expression);
            }

            SerializedProperty custom = vrm.FindProperty("Expression.CustomClips");
            if (custom == null || !custom.isArray)
            {
                return expressions;
            }

            for (int i = 0; i < custom.arraySize; i++)
            {
                Object clip = custom.GetArrayElementAtIndex(i).objectReferenceValue;
                if (clip == null)
                {
                    continue;
                }

                VrmExpressionData expression = ReadClip10(clip);
                if (string.IsNullOrEmpty(expression.Name))
                {
                    expression.Name = "Expression";
                }

                expressions.Add(expression);
            }

            return expressions;
        }

        /// <summary>The expressions a VRM 0.x avatar declares, from its blend shape avatar asset.</summary>
        public static List<VrmExpressionData> ReadExpressions0X(Component proxy)
        {
            List<VrmExpressionData> expressions = new List<VrmExpressionData>();
            SerializedObject avatar = ObjectAsset(proxy, "BlendShapeAvatar");
            SerializedProperty clips = avatar?.FindProperty("Clips");

            if (clips == null || !clips.isArray)
            {
                return expressions;
            }

            for (int i = 0; i < clips.arraySize; i++)
            {
                Object clip = clips.GetArrayElementAtIndex(i).objectReferenceValue;
                if (clip != null)
                {
                    expressions.Add(ReadClip0X(clip));
                }
            }

            return expressions;
        }

        private static VrmExpressionData ReadClip10(Object clip)
        {
            SerializedObject serialized = new SerializedObject(clip);
            VrmExpressionData expression = new VrmExpressionData { Name = clip.name };

            ReadBindings(serialized.FindProperty("MorphTargetBindings"), Vrm10WeightToUnity,
                expression);

            expression.MaterialBindingCount =
                Count(serialized.FindProperty("MaterialColorBindings"))
                + Count(serialized.FindProperty("MaterialUVBindings"));

            return expression;
        }

        private static VrmExpressionData ReadClip0X(Object clip)
        {
            SerializedObject serialized = new SerializedObject(clip);
            VrmExpressionData expression = new VrmExpressionData
            {
                Name = Text(serialized, "BlendShapeName"),
            };

            if (serialized.FindProperty("Preset") is SerializedProperty preset
                && preset.intValue >= 0 && preset.intValue < Vrm0Roles.Length)
            {
                expression.Role = Vrm0Roles[preset.intValue];
            }

            // A 0.x binding's weight is already on Unity's scale: UniVRM passes it straight to
            // SetBlendShapeWeight.
            ReadBindings(serialized.FindProperty("Values"), 1f, expression);
            expression.MaterialBindingCount = Count(serialized.FindProperty("MaterialValues"));

            if (string.IsNullOrEmpty(expression.Name))
            {
                expression.Name = string.IsNullOrEmpty(clip.name) ? "Expression" : clip.name;
            }

            return expression;
        }

        private static void ReadBindings(
            SerializedProperty bindings, float weightScale, VrmExpressionData expression)
        {
            if (bindings == null || !bindings.isArray)
            {
                return;
            }

            for (int i = 0; i < bindings.arraySize; i++)
            {
                SerializedProperty entry = bindings.GetArrayElementAtIndex(i);

                expression.Bindings.Add(new VrmMorphBinding
                {
                    Path = entry.FindPropertyRelative("RelativePath")?.stringValue ?? string.Empty,
                    Index = entry.FindPropertyRelative("Index")?.intValue ?? 0,
                    Weight = (entry.FindPropertyRelative("Weight")?.floatValue ?? 0f) * weightScale,
                });
            }
        }

        /// <summary>The asset a component points at, as something to read fields from.</summary>
        private static SerializedObject ObjectAsset(Component component, string field)
        {
            if (component == null)
            {
                return null;
            }

            Object asset = new SerializedObject(component)
                .FindProperty(field)?.objectReferenceValue;

            return asset == null ? null : new SerializedObject(asset);
        }

        private static SerializedProperty Relative(SerializedProperty parent, string name)
        {
            return parent?.FindPropertyRelative(name);
        }

        /// <summary>A yes or no field of a VRM 1.0 meta block.</summary>
        private static bool? Flag(SerializedProperty meta, string name)
        {
            SerializedProperty property = Relative(meta, name);
            return property == null ? (bool?)null : property.boolValue;
        }

        /// <summary>A VRM 0.x usage field, which is Disallow or Allow.</summary>
        private static bool? Allowed(SerializedObject asset, string name)
        {
            SerializedProperty property = asset.FindProperty(name);
            return property == null ? (bool?)null : property.intValue != 0;
        }

        private static int Count(SerializedProperty list)
        {
            return list == null || !list.isArray ? 0 : list.arraySize;
        }

        /// <summary>
        /// VRM 1.0 weights run from 0 to 1, and Unity's blendshape weights from 0 to 100.
        /// UniVRM's own constant for this is `MorphTargetBinding.VRM_TO_UNITY`.
        /// </summary>
        private const float Vrm10WeightToUnity = 100f;

        /// <summary>The presets of VRM 1.0, as the fields the object asset names them by.</summary>
        private static readonly (string Field, VrmExpressionRole Role)[] Vrm10Presets =
        {
            ("Happy", VrmExpressionRole.Emotion),
            ("Angry", VrmExpressionRole.Emotion),
            ("Sad", VrmExpressionRole.Emotion),
            ("Relaxed", VrmExpressionRole.Emotion),
            ("Surprised", VrmExpressionRole.Emotion),
            ("Aa", VrmExpressionRole.Viseme),
            ("Ih", VrmExpressionRole.Viseme),
            ("Ou", VrmExpressionRole.Viseme),
            ("Ee", VrmExpressionRole.Viseme),
            ("Oh", VrmExpressionRole.Viseme),
            ("Blink", VrmExpressionRole.Blink),
            ("BlinkLeft", VrmExpressionRole.Blink),
            ("BlinkRight", VrmExpressionRole.Blink),
            ("LookUp", VrmExpressionRole.LookAt),
            ("LookDown", VrmExpressionRole.LookAt),
            ("LookLeft", VrmExpressionRole.LookAt),
            ("LookRight", VrmExpressionRole.LookAt),
            ("Neutral", VrmExpressionRole.Neutral),
        };

        /// <summary>
        /// The presets of VRM 0.x, in the order its `BlendShapePreset` enum declares them.
        /// </summary>
        private static readonly VrmExpressionRole[] Vrm0Roles =
        {
            VrmExpressionRole.Custom,   // Unknown, named by the author instead
            VrmExpressionRole.Neutral,
            VrmExpressionRole.Viseme,   // A
            VrmExpressionRole.Viseme,   // I
            VrmExpressionRole.Viseme,   // U
            VrmExpressionRole.Viseme,   // E
            VrmExpressionRole.Viseme,   // O
            VrmExpressionRole.Blink,
            VrmExpressionRole.Emotion,  // Joy
            VrmExpressionRole.Emotion,  // Angry
            VrmExpressionRole.Emotion,  // Sorrow
            VrmExpressionRole.Emotion,  // Fun
            VrmExpressionRole.LookAt,   // LookUp
            VrmExpressionRole.LookAt,   // LookDown
            VrmExpressionRole.LookAt,   // LookLeft
            VrmExpressionRole.LookAt,   // LookRight
            VrmExpressionRole.Blink,    // Blink_L
            VrmExpressionRole.Blink,    // Blink_R
        };

        /// <summary>How far VRM 1.0 lets commercial use go, in its own words.</summary>
        private static readonly string[] Vrm10CommercialNames =
        {
            "personal, not for profit",
            "personal, including for profit",
            "corporate",
        };

        /// <summary>VRM 0.x's licence types, in the order its own enum declares them.</summary>
        private static readonly string[] Vrm0LicenseNames =
        {
            "Redistribution_Prohibited",
            "CC0",
            "CC_BY",
            "CC_BY_NC",
            "CC_BY_SA",
            "CC_BY_NC_SA",
            "CC_BY_ND",
            "CC_BY_NC_ND",
            "Other",
        };

        private static float Float(SerializedObject serialized, string name, float fallback)
        {
            SerializedProperty property = serialized.FindProperty(name);
            return property == null ? fallback : property.floatValue;
        }

        private static int Int(SerializedObject serialized, string name, int fallback)
        {
            SerializedProperty property = serialized.FindProperty(name);
            return property == null ? fallback : property.intValue;
        }

        private static Vector3 Vector(SerializedObject serialized, string name, Vector3 fallback)
        {
            SerializedProperty property = serialized.FindProperty(name);
            return property == null ? fallback : property.vector3Value;
        }

        private static string Text(SerializedObject serialized, string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            return property == null ? string.Empty : property.stringValue ?? string.Empty;
        }

        private static long Reference(SerializedObject serialized, string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            return property == null ? 0L : IdOf(property.objectReferenceValue);
        }

        private static List<long> References(SerializedObject serialized, string name)
        {
            return References(serialized.FindProperty(name));
        }

        private static List<long> References(SerializedProperty list)
        {
            List<long> ids = new List<long>();
            if (list == null || !list.isArray)
            {
                return ids;
            }

            for (int i = 0; i < list.arraySize; i++)
            {
                ids.Add(IdOf(list.GetArrayElementAtIndex(i).objectReferenceValue));
            }

            return ids;
        }
    }
}
