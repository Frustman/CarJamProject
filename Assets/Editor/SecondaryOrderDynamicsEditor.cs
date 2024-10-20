using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SecondOrderDynamics))]
public class SecondOrderDynamicsDrawer : PropertyDrawer
{
    private const int GraphWidth = 400;
    private const int GraphHeight = 150;
    private Material lineMaterial;
    private bool foldout;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        foldout = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), foldout, label);

        if (foldout)
        {
            EditorGUI.BeginProperty(position, label, property);

            position.y += EditorGUIUtility.singleLineHeight;

            Rect indentedPosition = EditorGUI.IndentedRect(position);
            indentedPosition.x += 10f;

            SerializedProperty fProp = property.FindPropertyRelative("f");
            SerializedProperty zProp = property.FindPropertyRelative("z");
            SerializedProperty rProp = property.FindPropertyRelative("r");
            SerializedProperty k1Prop = property.FindPropertyRelative("k1");
            SerializedProperty k2Prop = property.FindPropertyRelative("k2");
            SerializedProperty k3Prop = property.FindPropertyRelative("k3");

            indentedPosition.height = EditorGUIUtility.singleLineHeight;

            property.serializedObject.Update();

            fProp.floatValue = EditorGUI.Slider(new Rect(indentedPosition.x, indentedPosition.y, indentedPosition.width, EditorGUIUtility.singleLineHeight), "Frequency", fProp.floatValue, 0f, 10f);
            indentedPosition.y += EditorGUIUtility.singleLineHeight;
            zProp.floatValue = EditorGUI.Slider(new Rect(indentedPosition.x, indentedPosition.y, indentedPosition.width, EditorGUIUtility.singleLineHeight), "Zeta", zProp.floatValue, 0f, 10f);
            indentedPosition.y += EditorGUIUtility.singleLineHeight;
            rProp.floatValue = EditorGUI.Slider(new Rect(indentedPosition.x, indentedPosition.y, indentedPosition.width, EditorGUIUtility.singleLineHeight), "Response", rProp.floatValue, -10f, 10f);
            indentedPosition.y += EditorGUIUtility.singleLineHeight + 10;

            float f = fProp.floatValue;
            float z = zProp.floatValue;
            float r = rProp.floatValue;

            k1Prop.floatValue = z / (Mathf.PI * f);
            k2Prop.floatValue = 1 / ((2 * Mathf.PI * f) * (2 * Mathf.PI * f));
            k3Prop.floatValue = r * z / (2 * Mathf.PI * f);

            property.serializedObject.ApplyModifiedProperties();

            indentedPosition.y += EditorGUIUtility.singleLineHeight;

            Rect graphRect = new Rect(indentedPosition.x + 20f, indentedPosition.y, GraphWidth, GraphHeight);

            if (Event.current.type == EventType.Repaint)
            {
                if (lineMaterial == null)
                {
                    CreateLineMaterial();
                }

                float k1 = k1Prop.floatValue;
                float k2 = k2Prop.floatValue;
                float k3 = k3Prop.floatValue;

                DrawGraph(graphRect, k1, k2, k3);
            }

            EditorGUI.EndProperty();
        }
    }

    private void CreateLineMaterial()
    {
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
    }

    private void DrawGraph(Rect rect, float k1, float k2, float k3)
    {
        float deltaTime = 0.01f;
        float tMax = 2f;
        int steps = Mathf.CeilToInt(tMax / deltaTime);

        float yCurrent = 0f, yDelta = 0f;

        float xCurrent = 0f;
        float xPrevious = 0f;

        float yMin = float.MaxValue, yMax = float.MinValue;

        for (int i = 0; i <= steps; i++)
        {
            if (i * deltaTime >= 0.3f) // x = 0.3 에서의 step function이 Input
            {
                xCurrent = 1f;
            }

            float xDelta = (xCurrent - xPrevious) / deltaTime;
            xPrevious = xCurrent;

            float stableK2 = Mathf.Max(k2, 1.1f * (deltaTime * deltaTime / 4f + deltaTime * k1 / 2f));
            yCurrent += deltaTime * yDelta;
            yDelta += deltaTime * (xCurrent + k3 * xDelta - yCurrent - k1 * yDelta) / stableK2;

            if (yCurrent < yMin) yMin = yCurrent;
            if (yCurrent > yMax) yMax = yCurrent;
        }

        yMax = Mathf.Max(yMax, 1f);

        yCurrent = 0f;
        yDelta = 0f;
        xCurrent = 0f;
        xPrevious = 0f;

        float yScale = 1f;
        float yOffset = rect.height * (1 - yScale);

        GL.PushMatrix();
        lineMaterial.SetPass(0);
        GL.LoadPixelMatrix();

        GL.Begin(GL.QUADS);
        GL.Color(new Color(0, 0, 0, 0));
        GL.Vertex3(rect.x, rect.y, 0);
        GL.Vertex3(rect.x + rect.width, rect.y, 0);
        GL.Vertex3(rect.x + rect.width, rect.y + rect.height, 0);
        GL.Vertex3(rect.x, rect.y + rect.height, 0);
        GL.End();

        GL.Begin(GL.LINES);
        GL.Color(Color.white);
        GL.Vertex3(rect.x, rect.y + rect.height, 0);
        GL.Vertex3(rect.x + rect.width, rect.y + rect.height, 0);

        GL.Vertex3(rect.x, rect.y, 0);
        GL.Vertex3(rect.x, rect.y + rect.height, 0);
        GL.End();

        GL.Begin(GL.LINE_STRIP);
        GL.Color(Color.cyan);

        for (int i = 0; i <= steps; i++)
        {
            float t = i * deltaTime;
            float px = rect.x + (t / tMax) * rect.width;

            if (t >= 0.3f) // x = 0.3 에서의 step function이 Input
            {
                xCurrent = 1f;
            }

            float xDelta = (xCurrent - xPrevious) / deltaTime;
            xPrevious = xCurrent;

            float stableK2 = Mathf.Max(k2, 1.1f * (deltaTime * deltaTime / 4f + deltaTime * k1 / 2f));
            yCurrent += deltaTime * yDelta;
            yDelta += deltaTime * (xCurrent + k3 * xDelta - yCurrent - k1 * yDelta) / stableK2;

            float normalizedY = (yCurrent - yMin) / (yMax - yMin);
            float py = rect.y + rect.height - (normalizedY * rect.height * yScale + yOffset);

            GL.Vertex3(px, py, 0);
        }

        GL.End();
        GL.Begin(GL.LINES);
        GL.Color(Color.green);

        float y0 = rect.y + rect.height - (0 - yMin) / (yMax - yMin) * rect.height;
        GL.Vertex3(rect.x, y0, 0);
        GL.Vertex3(rect.x + rect.width, y0, 0);

        float y1 = rect.y + rect.height - (1 - yMin) / (yMax - yMin) * rect.height;
        GL.Vertex3(rect.x, y1, 0);
        GL.Vertex3(rect.x + rect.width, y1, 0);

        GL.End();

        GL.PopMatrix();

        Handles.BeginGUI();
        GUI.color = Color.white;
        Handles.Label(new Vector3(rect.x - 12f, y1 - 8f, 0), "1");
        Handles.Label(new Vector3(rect.x - 12f, y0 - 8f, 0), "0");
        Handles.EndGUI();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (foldout)
        {
            return EditorGUIUtility.singleLineHeight * 6 + GraphHeight + 20;
        }
        else
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}