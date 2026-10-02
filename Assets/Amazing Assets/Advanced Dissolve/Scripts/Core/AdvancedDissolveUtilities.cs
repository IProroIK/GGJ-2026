// Advanced Dissolve <https://u3d.as/16cX>
// Copyright (c) Amazing Assets <https://amazingassets.world>

using UnityEngine;


namespace AmazingAssets.AdvancedDissolve
{
    static public class Utilities
    {
        static public class Enum
        {
            public enum RenderPipeline { Unknown, Builtin, Universal, HighDefinition }
        }


        static public bool ShaderIsAdvancedDissolve(Shader shader)
        {
            if (shader == null)
                return false;

            int propertiesCount = shader.GetPropertyCount();
            for (int i = 0; i < propertiesCount; i++)
            {
                if (shader.GetPropertyName(i) == "_AdvancedDissolveCutoutStandardClip")
                    return true;
            }

            return false;
        }

        static public Enum.RenderPipeline GetCurrentRenderPipeline()
        {
            return Enum.RenderPipeline.Universal;
        }

        static public void Log(string message)
        {
            Log(LogType.Log, message, null, null);
        }
        static public void Log(LogType logType, string message)
        {
            Log(logType, message, null, null);
        }
        static public void Log(LogType logType, string message, System.Exception exception, UnityEngine.Object context = null)
        {
            message = "[Advanced Dissolve] " + message;

            StackTraceLogType save = Application.GetStackTraceLogType(logType);
            Application.SetStackTraceLogType(logType, StackTraceLogType.None);

            switch (logType)
            {
                case LogType.Assert:
                    {
                        if (context == null)
                            UnityEngine.Debug.LogAssertion(message);
                        else
                            UnityEngine.Debug.LogAssertion(message, context);

                    }
                    break;

                case LogType.Error:
                    {
                        if (context == null)
                            UnityEngine.Debug.LogError(message);
                        else
                            UnityEngine.Debug.LogError(message, context);
                    }
                    break;

                case LogType.Exception:
                    {
                        if (context == null)
                            UnityEngine.Debug.LogException(exception);
                        else
                            UnityEngine.Debug.LogException(exception, context);
                    }
                    break;

                case LogType.Log:
                    {
                        if (context == null)
                            UnityEngine.Debug.Log(message);
                        else
                            UnityEngine.Debug.Log(message, context);

                    }
                    break;

                case LogType.Warning:
                    {
                        if (context == null)
                            UnityEngine.Debug.LogWarning(message);
                        else
                            UnityEngine.Debug.LogWarning(message, context);

                    }
                    break;
            }

            Application.SetStackTraceLogType(logType, save);
        }
    }
}
