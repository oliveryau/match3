#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Match3
{
    [CustomPropertyDrawer(typeof(HomeVideoEntry))]
    public class HomeVideoEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            var mode = (HomeVideoPlaybackMode)property.FindPropertyRelative("mode").enumValueIndex;
            float line = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;
            float height = line + gap + (line + gap) * 5;

            if (mode == HomeVideoPlaybackMode.Segmented)
            {
                var segments = property.FindPropertyRelative("segments");
                height += EditorGUI.GetPropertyHeight(segments, true) + gap;
                height += (line + gap) * 2;

                var leftLevel = property.FindPropertyRelative("leftLevel");
                var rightLevel = property.FindPropertyRelative("rightLevel");
                height += EditorGUI.GetPropertyHeight(leftLevel, true) + gap;
                height += EditorGUI.GetPropertyHeight(rightLevel, true) + gap;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var idProp = property.FindPropertyRelative("id");
            string title = idProp.enumDisplayNames[Mathf.Clamp(idProp.enumValueIndex, 0, idProp.enumDisplayNames.Length - 1)];

            var foldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, title, true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            float y = foldRect.yMax + EditorGUIUtility.standardVerticalSpacing;
            float line = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;
            float width = position.width;

            y = DrawProp(property, "id", position.x, y, width, line, gap, new GUIContent("Id"));
            y = DrawProp(property, "clip", position.x, y, width, line, gap);
            y = DrawProp(property, "mode", position.x, y, width, line, gap);
            y = DrawProp(property, "loop", position.x, y, width, line, gap);
            y = DrawProp(property, "mute", position.x, y, width, line, gap, new GUIContent("Mute Audio"));

            var mode = (HomeVideoPlaybackMode)property.FindPropertyRelative("mode").enumValueIndex;
            if (mode == HomeVideoPlaybackMode.Segmented)
            {
                var segments = property.FindPropertyRelative("segments");
                float segHeight = EditorGUI.GetPropertyHeight(segments, true);
                EditorGUI.PropertyField(new Rect(position.x, y, width, segHeight), segments, true);
                y += segHeight + gap;

                y = DrawProp(property, "leftButtonSprite", position.x, y, width, line, gap, new GUIContent("Left Button Sprite"));
                y = DrawProp(property, "rightButtonSprite", position.x, y, width, line, gap, new GUIContent("Right Button Sprite"));

                var leftLevel = property.FindPropertyRelative("leftLevel");
                float leftHeight = EditorGUI.GetPropertyHeight(leftLevel, true);
                EditorGUI.PropertyField(new Rect(position.x, y, width, leftHeight), leftLevel, new GUIContent("Left Level"), true);
                y += leftHeight + gap;

                var rightLevel = property.FindPropertyRelative("rightLevel");
                float rightHeight = EditorGUI.GetPropertyHeight(rightLevel, true);
                EditorGUI.PropertyField(new Rect(position.x, y, width, rightHeight), rightLevel, new GUIContent("Right Level"), true);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        static float DrawProp(
            SerializedProperty root,
            string name,
            float x,
            float y,
            float width,
            float line,
            float gap,
            GUIContent label = null)
        {
            var prop = root.FindPropertyRelative(name);
            if (label == null)
                EditorGUI.PropertyField(new Rect(x, y, width, line), prop);
            else
                EditorGUI.PropertyField(new Rect(x, y, width, line), prop, label);
            return y + line + gap;
        }
    }
}
#endif
