using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Container for an integer variable reference or constant value.
    /// </summary>
    [System.Serializable]
    [VariableData(typeof(int), typeof(IVariable<int>))]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class IntegerData : VariableData<int>
    {
        [SerializeField]
        [VariableProperty("<Value>", typeof(IntegerVariable))]
        public IntegerVariable integerRef;

        [SerializeField]
        public int integerVal;

        protected override Variable LegacyVarRef
        {
            get => integerRef;
            set
            {
                integerRef = value as IntegerVariable;
                base.LegacyVarRef = value;
            }
        }

        protected override int LegacyLiteralVal
        {
            get => integerVal;
            set => integerVal = value;
        }

        public IntegerData() : base(default) { }

        public IntegerData(int startVal) : base(startVal)
        {
        }
    }

    /// <summary>
    /// Container for a float variable reference or constant value.
    /// </summary>
    [VariableData(typeof(float), typeof(IVariable<float>))]
    [System.Serializable]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class FloatData : VariableData<float>
    {
        [SerializeField]
        [VariableProperty("<Value>", typeof(FloatVariable))]
        public FloatVariable floatRef;

        [SerializeField]
        public float floatVal;

        protected override Variable LegacyVarRef
        {
            get => floatRef;
            set
            {
                floatRef = value as FloatVariable;
                base.LegacyVarRef = value;
            }
        }

        protected override float LegacyLiteralVal
        {
            get => floatVal;
            set => floatVal = value;
        }

        public FloatData() : base(default) { }

        public FloatData(float startVal) : base(startVal)
        {
        }
    }

    /// <summary>
    /// Container for a number variable reference or constant value. Takes over from
    /// <see cref="DoubleData"/> as the registered data type for doubles, and works with any
    /// numeric variable (the same way FloatData and IntegerData do), NumberMuscariable
    /// included.
    /// </summary>
    [VariableData(typeof(double), typeof(IVariable<double>))]
    [System.Serializable]
    public class NumberData : VariableData<double>
    {
        public NumberData() : base(default) { }
        public NumberData(double startVal) : base(startVal) { }
    }

    /// <summary>
    /// Container for a double variable reference or constant value. Superseded by
    /// <see cref="NumberData"/>, so it no longer claims any variable types in the registry
    /// (the registry keeps one data type per variable type). It stays so that data saved
    /// as one still deserializes.
    /// </summary>
    [VariableData(typeof(double))]
    [System.Serializable]
    public class DoubleData : VariableData<double>
    {
        [SerializeField]
        public double doubleVal;

        protected override double LegacyLiteralVal
        {
            get => doubleVal;
            set => doubleVal = value;
        }
        public DoubleData() : base(default) { }
        public DoubleData(double startVal) : base(startVal)
        {
        }
    }

    /// <summary>
    /// Container for a Boolean variable reference or constant value.
    /// </summary>
    [System.Serializable]
    [VariableData(typeof(bool), typeof(IVariable<bool>))]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class BooleanData : VariableData<bool>
    {
        [SerializeField]
        [VariableProperty("<Value>", typeof(BooleanVariable))]
        public BooleanVariable booleanRef;

        protected override Variable LegacyVarRef
        {
            get => booleanRef;
            set => booleanRef = value as BooleanVariable;
        }

        protected override bool LegacyLiteralVal
        {
            get => booleanVal;
            set => booleanVal = value;
        }

        [SerializeField]
        public bool booleanVal;

        public BooleanData() : base(default) { }
        public BooleanData(bool startVal = default) : base(startVal) { }

        public static implicit operator bool(BooleanData booleanData)
        {
            return booleanData.Value;
        }
    }

    /// <summary>
    /// Container for a vector variable reference or constant value, for use with
    /// VectorMuscariable. Always holds a Vector4. Vector2s and Vector3s that get assigned
    /// are padded with zeroes, and any vector variable can be referenced (see
    /// <see cref="VectorConversion"/>).
    /// </summary>
    [System.Serializable]
    [VariableData(typeof(Vector4), typeof(IVariable<Vector4>))]
    public class VectorData : VariableData<Vector4>
    {
        public VectorData() : base(default) { }
        public VectorData(Vector4 startVal) : base(startVal) { }
    }

    /// <summary>
    /// Container for a Vector2 variable reference or constant value.
    /// </summary>
    [System.Serializable]
    [VariableData(typeof(Vector2), typeof(IVariable<Vector2>))]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Vector2Data : VariableData<Vector2>
    {
        [SerializeField]
        [VariableProperty("<Value>", typeof(Vector2Variable))]
        public Vector2Variable vector2Ref;

        [SerializeField]
        public Vector2 vector2Val;

        public Vector2Data() : base(default) { }
        public Vector2Data(Vector2 startVal = default) : base(startVal) { }

        protected override Variable LegacyVarRef
        {
            get => vector2Ref;
            set => vector2Ref = value as Vector2Variable;
        }

        protected override Vector2 LegacyLiteralVal
        {
            get => vector2Val;
            set => vector2Val = value;
        }
    }

    /// <summary>
    /// Container for a Vector3 variable reference or constant value.
    /// </summary>
    [System.Serializable]
    [VariableData(typeof(Vector3), typeof(IVariable<Vector3>))]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Vector3Data : VariableData<Vector3>
    {
        [SerializeField]
        [VariableProperty("<Value>", typeof(Vector3Variable))]
        public Vector3Variable vector3Ref;

        [SerializeField]
        public Vector3 vector3Val;

        public Vector3Data() : base(default) { }
        public Vector3Data(Vector3 startVal = default) : base(startVal) { }

        protected override Variable LegacyVarRef
        {
            get => vector3Ref;
            set => vector3Ref = value as Vector3Variable;
        }

        protected override Vector3 LegacyLiteralVal
        {
            get => vector3Val;
            set => vector3Val = value;
        }

    }
}