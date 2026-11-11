using System;

namespace Umbral.Core
{
    // Modelos del catálogo. Los nombres de campo coinciden con server/seed/catalog.json
    // (JsonUtility los lee tal cual, por eso son campos públicos y arrays).

    [Serializable]
    public class ProjectInfo
    {
        public string id;
        public string name;
    }

    [Serializable]
    public class UnitInfo
    {
        public string id;
        public int floor;
        public float areaM2;
        public long basePrice; // bolivianos enteros
    }

    [Serializable]
    public class OptionInfo
    {
        public string id;
        public string name;
        public long delta; // diferencia frente a la opción incluida (la primera tiene delta 0)
    }

    [Serializable]
    public class SurfaceInfo
    {
        public string id;
        public string name;
        public OptionInfo[] options; // la primera es la opción por defecto
    }

    [Serializable]
    public class Catalog
    {
        public int version;
        public string validUntil; // yyyy-MM-dd, se muestra en el visor
        public ProjectInfo project;
        public UnitInfo[] units;
        public SurfaceInfo[] surfaces;
    }
}
