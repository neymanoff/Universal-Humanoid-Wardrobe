using UnityEditor;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe.Editor
{
    [CustomEditor(typeof(WardrobeManager))]
    public class WardrobeManagerEditor : UnityEditor.Editor
    {
        private WardrobeManager _manager;

        private void OnEnable()
        {
            _manager = (WardrobeManager)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Editor Preview Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview Default Loadout", GUILayout.Height(30)))
                {
                    PreviewLoadoutInEditor();
                }
                if (GUILayout.Button("Clear Preview", GUILayout.Height(30)))
                {
                    ClearPreviewInEditor();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void PreviewLoadoutInEditor()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            ClearPreviewInEditor();

            foreach (var item in _manager.defaultLoadout)
            {
                if (!item.prefab) continue;
                
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(item.prefab);
                
                Undo.RegisterCreatedObjectUndo(instance, $"Equip Default {item.slot}");

                _manager.Equip(item.slot, instance);
                
                Undo.SetTransformParent(instance.transform, instance.transform.parent, "Reparent Equipped Item");
            }
            
            Undo.CollapseUndoOperations(undoGroup);
            
            EditorUtility.SetDirty(_manager.gameObject);
        }

        private void ClearPreviewInEditor()
        {
            var slots = System.Enum.GetValues(typeof(EquipmentSlot));
            System.Collections.Generic.List<GameObject> toDestroy = new();

            for (int i = 0; i < _manager.transform.childCount; i++)
            {
                Transform child = _manager.transform.GetChild(i);
                foreach (EquipmentSlot slot in slots)
                {
                    if (child.name.EndsWith($"_{slot}"))
                    {
                        toDestroy.Add(child.gameObject);
                    }
                }
            }
            
            Animator animator = _manager.GetComponent<Animator>();
            if (animator && animator.isHuman)
            {
                foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
                {
                    if (bone == HumanBodyBones.LastBone) continue;
                    Transform boneTransform = animator.GetBoneTransform(bone);
                    if (!boneTransform) continue;

                    for (int i = 0; i < boneTransform.childCount; i++)
                    {
                        Transform child = boneTransform.GetChild(i);
                        foreach (EquipmentSlot slot in slots)
                        {
                            if (child.name.EndsWith($"_{slot}"))
                            {
                                toDestroy.Add(child.gameObject);
                            }
                        }
                    }
                }
            }

            foreach (var obj in toDestroy)
            {
                if (obj)
                {
                    Undo.DestroyObjectImmediate(obj);
                }
            }
            
            _manager.UnequipAll();
            EditorUtility.SetDirty(_manager.gameObject);
        }
    }
}