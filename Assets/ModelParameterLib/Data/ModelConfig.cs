using System.Collections.Generic;

namespace ModelParameterLib.Data
{
    [System.Serializable]
    public class ModelConfig
    {
        public PositionConfig positionConfig = new PositionConfig();
        public List<MaterialMapping> materialMappings = new List<MaterialMapping>();
        public ModelParameters parameters = new ModelParameters();
        public InteractionConfig interactionConfig = new InteractionConfig();
        public CustomSettings customSettings = new CustomSettings();
    }

    [System.Serializable]
    public class PositionConfig
    {
        public UnityEngine.Vector3 defaultPosition = UnityEngine.Vector3.zero;
        public UnityEngine.Vector3 defaultRotation = UnityEngine.Vector3.zero;
        public UnityEngine.Vector3 defaultScale = UnityEngine.Vector3.one;
        public bool snapToGround = false;
        public bool alignToSurface = false;
    }

    [System.Serializable]
    public class MaterialMapping
    {
        public string originalMaterial = "";
        public string materialPath = "";
    }

    [System.Serializable]
    public class ModelParameters
    {
        public PhysicsConfig physics = new PhysicsConfig();
    }

    [System.Serializable]
    public class PhysicsConfig
    {
        public bool hasCollider = false;
        public string colliderType = "box";
        public bool useGravity = false;
        public bool isStatic = true;
    }

    [System.Serializable]
    public class InteractionConfig
    {
        // 可根据需要扩展
    }

    [System.Serializable]
    public class CustomSettings
    {
        // 可根据需要扩展
    }
} 