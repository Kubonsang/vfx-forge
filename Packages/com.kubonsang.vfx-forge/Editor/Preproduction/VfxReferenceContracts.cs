using System;

namespace Kubonsang.VfxForge.Editor
{
    [Serializable]
    public sealed class VfxReferenceBoard
    {
        public string schemaVersion = "reference-board-1.0";
        public string id = string.Empty;
        public string taskId = string.Empty;
        public string title = string.Empty;
        public VfxReferenceItem[] references = Array.Empty<VfxReferenceItem>();
        public string[] styleGoals = Array.Empty<string>();
        public string[] globalAvoids = Array.Empty<string>();
    }

    [Serializable]
    public sealed class VfxReferenceItem
    {
        public string id = string.Empty;
        public string title = string.Empty;
        public string sourceType = string.Empty;
        public string source = string.Empty;
        public string creator = string.Empty;
        public string license = string.Empty;
        public string usage = string.Empty;
        public string[] desiredElements = Array.Empty<string>();
        public string[] avoidElements = Array.Empty<string>();
        public VfxReferenceFrame frame = new VfxReferenceFrame();
        public string notes = string.Empty;
    }

    [Serializable]
    public sealed class VfxReferenceFrame
    {
        public string cameraAngle = string.Empty;
        public string cropFocus = string.Empty;
    }

    [Serializable]
    public sealed class VfxArtDirectionBrief
    {
        public string schemaVersion = "art-direction-brief-1.0";
        public string id = string.Empty;
        public string taskId = string.Empty;
        public string referenceBoardId = string.Empty;
        public string referenceBoardSha256 = string.Empty;
        public string effectIntent = string.Empty;
        public int candidateCount = 3;
        public VfxConceptCamera camera = new VfxConceptCamera();
        public VfxSilhouetteDirection silhouette =
            new VfxSilhouetteDirection();
        public VfxDepthLayer[] depthLayers = Array.Empty<VfxDepthLayer>();
        public VfxMaterialZone[] materialZones = Array.Empty<VfxMaterialZone>();
        public string[] forbiddenTraits = Array.Empty<string>();
        public string[] acceptanceQuestions = Array.Empty<string>();
        public VfxConceptOutputRequirements outputs =
            new VfxConceptOutputRequirements();
    }

    [Serializable]
    public sealed class VfxConceptCamera
    {
        public string view = "strict_top_down";
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;
        public int effectFootprintPixels = 320;
    }

    [Serializable]
    public sealed class VfxSilhouetteDirection
    {
        public string primaryMass = string.Empty;
        public float primaryMassRatio = 0.65f;
        public float secondaryMassRatio = 0.25f;
        public float negativeSpaceRatio = 0.1f;
        public string asymmetry = string.Empty;
        public string[] connections = Array.Empty<string>();
        public string[] motifs = Array.Empty<string>();
    }

    [Serializable]
    public sealed class VfxDepthLayer
    {
        public string id = string.Empty;
        public string role = string.Empty;
        public int order;
    }

    [Serializable]
    public sealed class VfxMaterialZone
    {
        public string id = string.Empty;
        public string role = string.Empty;
        public string finish = string.Empty;
    }

    [Serializable]
    public sealed class VfxConceptOutputRequirements
    {
        public bool grayscaleSilhouette = true;
        public bool fullColorConcept = true;
        public bool threeGroundComposite = true;
        public bool labeledBreakdown = true;
    }
}
