using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    [Serializable]
    [MovedFrom(true,
        "AtMycelia.Hyphlow",
        "AtMycelia.Amanita.Core")]
    public abstract class NumericMuscariable<T> : Muscariable<T>, IComparable<T>,
        IComparable<NumericMuscariable<T>>
        where T : IComparable<T>, IEquatable<T>
    {
        public override bool IsArithmeticSupported(SetOperator op) => true;
        public override bool IsRelationalSupported => true;
        public override bool IsComparisonSupported() => true;

        public override void Apply(SetOperator setOperator, T toApply)
        {
            switch (setOperator)
            {
                case SetOperator.Assign:
                    Value = toApply;
                    break;

                case SetOperator.Add:
                    Value = _Add(Value, toApply);
                    break;

                case SetOperator.Subtract:
                    Value = _Sub(Value, toApply);
                    break;

                case SetOperator.Multiply:
                    Value = _Mul(Value, toApply);
                    break;

                case SetOperator.Divide:
                    Value = _Div(Value, toApply);
                    break;

                case SetOperator.Negate:
                    Value = _Neg(Value);
                    break;

                default:
                    Debug.LogError($"The {setOperator} set operator is not valid for {ContentType.Name} variable {Key}.");
                    break;
            }
        }

        // Need these funcs so that we can perform the arithmetic operations
        // in Apply without needing to know the specific type of T (or use
        // the dynamic keyword, which is something we want to avoid for
        // the sake of supporting Unity 2022.3).
        protected static Func<T, T, T> _Add;
        protected static Func<T, T, T> _Sub;
        protected static Func<T, T, T> _Mul;
        protected static Func<T, T, T> _Div;
        protected static Func<T, T> _Neg;

        public override bool Evaluate(CompareOperator op, T otherNumericValue)
        {
            bool result;

            var comparisonRes = this.Value.CompareTo(otherNumericValue);
            switch (op)
            {
                case CompareOperator.Equals:
                    result = comparisonRes == 0; break;
                case CompareOperator.NotEquals:
                    result = comparisonRes != 0; break;
                case CompareOperator.LessThan:
                    result = comparisonRes < 0; break;
                case CompareOperator.GreaterThan:
                    result = comparisonRes > 0; break;
                case CompareOperator.LessThanOrEquals:
                    result = comparisonRes <= 0; break;
                case CompareOperator.GreaterThanOrEquals:
                    result = comparisonRes >= 0; break;
                default:
                    string errorMessage = $"Muscariable<{typeof(T).Name}> {Key} not compatible with CompareOperator {op}";
                    throw new System.ArgumentException(errorMessage);
            }

            return result;
        }

        public virtual int CompareTo(T numericValue)
        {
            return Value.CompareTo(numericValue);
        }

        public virtual int CompareTo(NumericMuscariable<T> otherNumericVar)
        {
            return Value.CompareTo(otherNumericVar.Value);
        }

    }

    [Serializable]
    [VariableInfo("Numeric", "Integer", typeof(int), showInMenu: false)]
    [MovedFrom(true,
        "AtMycelia.Hyphlow",
        "AtMycelia.Amanita.Core",
        "IntMuscariable")]
    public class IntMuscariable : NumericMuscariable<int>, IVariable<int>
    {
        static IntMuscariable()
        {
            _Add = (a, b) => a + b;
            _Sub = (a, b) => a - b;
            _Mul = (a, b) => a * b;
            _Div = (a, b) => a / b;
            _Neg = a => -a;
        }

        public IntMuscariable() : base() { }

        public static IntMuscariable operator +(IntMuscariable a, IntMuscariable b)
            => new IntMuscariable { Value = a.Value + b.Value };

        public static IntMuscariable operator -(IntMuscariable a, IntMuscariable b)
            => new IntMuscariable { Value = a.Value - b.Value };

        public static IntMuscariable operator *(IntMuscariable a, IntMuscariable b)
            => new IntMuscariable { Value = a.Value * b.Value };

        public static IntMuscariable operator /(IntMuscariable a, IntMuscariable b)
            => new IntMuscariable { Value = a.Value / b.Value };

        public static bool operator ==(IntMuscariable a, IntMuscariable b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.Value == b.Value;
        }

        public static bool operator !=(IntMuscariable a, IntMuscariable b)
        {
            if (ReferenceEquals(a, b)) return false;
            if (!ReferenceEquals(a, null) && !ReferenceEquals(b, null)) return a.Value != b.Value;
            return true;
        }

        public override bool Equals(object obj)
        {
            var other = obj as IntMuscariable;
            if (ReferenceEquals(other, null)) return false;
            return this.Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }


    }

    [Serializable]
    [VariableInfo("Numeric", "Float", typeof(float), showInMenu: false)]
    [MovedFrom(true, "AtMycelia.Hyphlow",
        "AtMycelia.Amanita.Core",
        "FloatMuscariable")]
    public class FloatMuscariable : NumericMuscariable<float>
    {
        static FloatMuscariable()
        {
            _Add = (a, b) => a + b;
            _Sub = (a, b) => a - b;
            _Mul = (a, b) => a * b;
            _Div = (a, b) => a / b;
            _Neg = a => -a;
        }

        public FloatMuscariable() : base() { }

        public static FloatMuscariable operator +(FloatMuscariable a, FloatMuscariable b)
            => new FloatMuscariable { Value = a.Value + b.Value };

        public static FloatMuscariable operator -(FloatMuscariable a, FloatMuscariable b)
            => new FloatMuscariable { Value = a.Value - b.Value };

        public static FloatMuscariable operator *(FloatMuscariable a, FloatMuscariable b)
            => new FloatMuscariable { Value = a.Value * b.Value };

        public static FloatMuscariable operator /(FloatMuscariable a, FloatMuscariable b)
            => new FloatMuscariable { Value = a.Value / b.Value };

        public static bool operator ==(FloatMuscariable a, FloatMuscariable b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.Value == b.Value;
        }

        public static bool operator !=(FloatMuscariable a, FloatMuscariable b)
        {
            if (ReferenceEquals(a, b)) return false;
            if (!ReferenceEquals(a, null) && !ReferenceEquals(b, null)) return a.Value != b.Value;
            return true;
        }

        public override bool Equals(object obj)
        {
            var other = obj as FloatMuscariable;
            if (ReferenceEquals(other, null)) return false;
            return this.Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }


    }

    [Serializable]
    [VariableInfo("Numeric", "Boolean", typeof(bool))]
    [MovedFrom(true, "AtMycelia.Hyphlow",
        "AtMycelia.Amanita.Core",
        "BoolMuscariable")]
    public class BoolMuscariable : NumericMuscariable<bool>
    {
        public override bool IsArithmeticSupported(SetOperator op) => false;

        public BoolMuscariable() : base() { }

        public override void Apply(SetOperator setOperator, bool toApply)
        {
            switch (setOperator)
            {
                case SetOperator.Assign:
                    Value = toApply;
                    break;
                case SetOperator.Negate:
                    Value = !toApply;
                    break;

                default:
                    Debug.LogError($"The {setOperator} set operator is not valid for {ContentType.Name} variable {Key}.");
                    break;
            }
        }

        public static bool operator ==(BoolMuscariable a, BoolMuscariable b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.Value == b.Value;
        }

        public static bool operator !=(BoolMuscariable a, BoolMuscariable b)
        {
            if (ReferenceEquals(a, b)) return false;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            var other = obj as BoolMuscariable;
            if (ReferenceEquals(other, null)) return false;
            return this.Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

    }

    /// <summary>
    /// The one variable type for plain numbers, meant to replace <see cref="IntMuscariable"/>,
    /// <see cref="FloatMuscariable"/> and <see cref="DoubleMuscariable"/> so users don't have to
    /// pick between them. Stores a double; with <see cref="WholeNumsOnly"/> on, assigned values
    /// lose their fractional part (truncated toward zero) instead of being kept as-is.
    /// </summary>
    [Serializable]
    [VariableInfo("Numeric", "Number", typeof(double))]
    public class NumberMuscariable : NumericMuscariable<double>
    {
        [SerializeField]
        protected bool _wholeNumsOnly;

        /// <summary>
        /// Default tolerance used when comparing floating-point values.
        /// </summary>
        public const double ComparisonTolerance = 1e-9;

        /// <summary>
        /// Returns true if the supplied values are close enough to be considered equal.
        /// Useful for visual scripting comparisons involving floating-point math.
        /// </summary>
        public static bool ApproximatelyEqual(double first, double second)
        {
            if (double.IsNaN(first) || double.IsNaN(second))
            {
                Debug.LogWarning($"Either the first or second double being compared is NaN");
                return false;
            }

            double absFirst = Math.Abs(first);
            double absSecond = Math.Abs(second);

            double bigger = Math.Max(absFirst, absSecond);

            double tolerance = ComparisonTolerance * Math.Max(1.0, bigger);
            bool result = Math.Abs(first - second) <= tolerance;

            return result;
        }

        static NumberMuscariable()
        {
            _Add = (first, second) => first + second;
            _Sub = (first, second) => first - second;
            _Mul = (first, second) => first * second;
            _Div = (first, second) => first / second;
            _Neg = first => -first;
        }

        public NumberMuscariable() : base() { }

        public virtual bool WholeNumsOnly
        {
            get => _wholeNumsOnly;
            set
            {
                if (_wholeNumsOnly == value)
                {
                    return;
                }

                _wholeNumsOnly = value;
                // Re-run the value through the setter so turning this on drops the current fraction 
                Value = _value;
            }
        }

        public override double Value
        {
            get => _value;
            set => base.Value = Filter(value);
        }

        // Muscariable<T>.BoxedValue writes straight to _value, bypassing Value, so it
        // needs its own pass through the filter.
        protected override object FilterForValueSet(object valueToConvert)
        {
            object filtered = base.FilterForValueSet(valueToConvert);
            if (!_wholeNumsOnly || filtered == null || filtered is not IConvertible)
            {
                return filtered;
            }

            double converted = Convert.ToDouble(filtered);
            return Filter(converted);
        }

        public override void OnReset()
        {
            _value = Filter(_startValue);
            TriggerOnValueChanged();
        }

        public override Muscariable Clone()
        {
            return new NumberMuscariable
            {
                _scope = _scope,
                _key = _key,
                _itemId = _itemId,
                _wholeNumsOnly = _wholeNumsOnly,
                _startValue = _startValue,
                _value = _value
            };
        }

        // Values this close (relative to their magnitude) to a whole number are treated as that
        // number, so floating-point noise like 2.9999999999999996 doesn't lose a whole unit
        // when the fraction is dropped.
        public const double WholeNumTolerance = 1e-9;

        private double Filter(double toFilter)
        {
            // Prevent invalid floating-point values from propagating
            // through visual-script calculations.
            string logMessage = "", cType = ContentType.Name;
            if (double.IsNaN(toFilter))
            {
                logMessage = $"NaN was assigned to {cType} variable {Key}; using 0 instead.";
                Debug.LogWarning(logMessage);
                return 0.0;
            }

            if (double.IsInfinity(toFilter))
            {
                logMessage = $"{toFilter} was assigned to {cType} variable {Key}; clamping it " +
                "to the nearest finite value.";
                Debug.LogWarning(logMessage);
                return toFilter > 0
                    ? double.MaxValue
                    : double.MinValue;
            }

            if (!_wholeNumsOnly)
            {
                return toFilter;
            }

            double nearest = Math.Round(toFilter);
            double tolerance = WholeNumTolerance * Math.Max(1.0, Math.Abs(toFilter));
            double result = Math.Abs(toFilter - nearest) <= tolerance ? 
            nearest : 
            Math.Truncate(toFilter);
            return result;
        }

        public static NumberMuscariable operator +(NumberMuscariable a, NumberMuscariable b)
            => new NumberMuscariable { WholeNumsOnly = a.WholeNumsOnly, Value = a.Value + b.Value };

        public static NumberMuscariable operator -(NumberMuscariable a, NumberMuscariable b)
            => new NumberMuscariable { WholeNumsOnly = a.WholeNumsOnly, Value = a.Value - b.Value };

        public static NumberMuscariable operator *(NumberMuscariable a, NumberMuscariable b)
            => new NumberMuscariable { WholeNumsOnly = a.WholeNumsOnly, Value = a.Value * b.Value };

        public static NumberMuscariable operator /(NumberMuscariable a, NumberMuscariable b)
            => new NumberMuscariable { WholeNumsOnly = a.WholeNumsOnly, Value = a.Value / b.Value };

        public static bool operator ==(NumberMuscariable first, NumberMuscariable second)
        {
            if (ReferenceEquals(first, second)) return true;
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null)) return false;
            return first.Value == second.Value;
        }

        public static bool operator !=(NumberMuscariable first, NumberMuscariable second)
        {
            if (ReferenceEquals(first, second)) return false;
            if (!ReferenceEquals(first, null) && !ReferenceEquals(second, null)) return first.Value != second.Value;
            return true;
        }

        public override bool Equals(object obj)
        {
            var other = obj as NumberMuscariable;
            if (ReferenceEquals(other, null)) return false;
            return this.Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    // Superseded by NumericMuscariable, which now owns the double content type. Kept (and
    // hidden from the menu) so scenes and assets that already hold one still deserialize.
    [Serializable]
    [VariableInfo("Numeric", "Double", typeof(double), showInMenu: false)]
    [MovedFrom(true, "AtMycelia.Hyphlow",
        "AtMycelia.Amanita.Core",
        "DoubleMuscariable")]
    public class DoubleMuscariable : NumericMuscariable<double>
    {
        static DoubleMuscariable()
        {
            _Add = (a, b) => a + b;
            _Sub = (a, b) => a - b;
            _Mul = (a, b) => a * b;
            _Div = (a, b) => a / b;
            _Neg = a => -a;
        }

        public DoubleMuscariable() : base() { }

        public static DoubleMuscariable operator +(DoubleMuscariable a, DoubleMuscariable b)
            => new DoubleMuscariable { Value = a.Value + b.Value };

        public static DoubleMuscariable operator -(DoubleMuscariable a, DoubleMuscariable b)
            => new DoubleMuscariable { Value = a.Value - b.Value };

        public static DoubleMuscariable operator *(DoubleMuscariable a, DoubleMuscariable b)
            => new DoubleMuscariable { Value = a.Value * b.Value };

        public static DoubleMuscariable operator /(DoubleMuscariable a, DoubleMuscariable b)
            => new DoubleMuscariable { Value = a.Value / b.Value };

        public static bool operator ==(DoubleMuscariable a, DoubleMuscariable b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.Value == b.Value;
        }

        public static bool operator !=(DoubleMuscariable a, DoubleMuscariable b)
        {
            if (ReferenceEquals(a, b)) return false;
            if (!ReferenceEquals(a, null) && !ReferenceEquals(b, null)) return a.Value != b.Value;
            return true;
        }

        public override bool Equals(object obj)
        {
            var other = obj as DoubleMuscariable;
            if (ReferenceEquals(other, null)) return false;
            return this.Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

    }

    [Serializable]
    [VariableInfo("Numeric/Structured", "VectorTwo", typeof(Vector2), showInMenu: false)]
    [MovedFrom(true, "AtMycelia.Hyphlow",
        "AtMycelia.Amanita.Core",
        "VectorTwoMuscariable")]
    public class VectorTwoMuscariable : Muscariable<Vector2>
    {
        public VectorTwoMuscariable() : base() { }

        public override bool IsArithmeticSupported(SetOperator op) => true;
        public override bool IsRelationalSupported => true;

        public virtual float X
        {
            get => Value.x;
            set
            {
                Vector2 newVal = Value;
                newVal.x = value;
                Value = newVal;
                TriggerOnValueChanged();
            }
        }

        public virtual float Y
        {
            get => Value.y;
            set
            {
                Vector2 newVal = Value;
                newVal.y = value;
                Value = newVal;
                TriggerOnValueChanged();
            }
        }

        public static VectorTwoMuscariable operator +(VectorTwoMuscariable a, VectorTwoMuscariable b)
        {
            VectorTwoMuscariable result = CloneMeta(a);
            result.Value += b.Value;
            return result;
        }

        public static VectorTwoMuscariable operator -(VectorTwoMuscariable a, VectorTwoMuscariable b)
        {
            VectorTwoMuscariable result = CloneMeta(a);
            result.Value -= b.Value;
            return result;
        }

        public static VectorTwoMuscariable operator *(VectorTwoMuscariable a, int intVal)
        {
            VectorTwoMuscariable result = CloneMeta(a);
            result.Value *= intVal;
            return result;
        }

        public static VectorTwoMuscariable operator *(VectorTwoMuscariable a, float floatVal)
        {
            VectorTwoMuscariable result = CloneMeta(a);
            result.Value *= floatVal;
            return result;
        }

        public static VectorTwoMuscariable operator *(VectorTwoMuscariable a, IntMuscariable intVar)
        {
            VectorTwoMuscariable result = CloneMeta(a);
            result.Value *= intVar.Value;
            return result;
        }

        public static VectorTwoMuscariable operator *(VectorTwoMuscariable a, FloatMuscariable floatVar)
        {
            VectorTwoMuscariable result = CloneMeta(a);
            result.Value *= floatVar.Value;
            return result;
        }

        public static bool operator ==(VectorTwoMuscariable a, VectorTwoMuscariable b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.Value == b.Value;
        }

        public static bool operator ==(VectorTwoMuscariable a, VectorThreeMuscariable b)
        {
            if (a is null || b is null) return false;
            return a.X == b.X &&
            a.Y == b.Y &&
            b.Z == 0; // Since in a 3D environment, VectorTwos are meant to be treated as if they have a Z of 0
        }


        public static bool operator !=(VectorTwoMuscariable a, VectorTwoMuscariable b)
        {
            if (ReferenceEquals(a, b)) return false;
            if (!ReferenceEquals(a, null) && !ReferenceEquals(b, null)) return a.Value != b.Value;
            return a.Value == b.Value;
        }

        public static bool operator !=(VectorTwoMuscariable a, VectorThreeMuscariable b)
        {
            bool oneIsNullTheOtherIsnt = (ReferenceEquals(a, null) && !ReferenceEquals(b, null)) ||
                (ReferenceEquals(b, null) && !ReferenceEquals(a, null));
            if (oneIsNullTheOtherIsnt) return true;

            bool bothAreValid = !ReferenceEquals(a, null) && !ReferenceEquals(b, null);

            if (bothAreValid)
            {
                return a.X != b.X ||
                    a.Y != b.Y ||
                    b.Z != 0;
            }

            return true;
        }

        public static VectorTwoMuscariable CloneMeta(VectorTwoMuscariable src)
        {
            return new VectorTwoMuscariable
            {
                _scope = src._scope,
                _key = src._key,
                _itemId = src._itemId,
                _value = src._value
            };
        }

        public override bool Equals(object obj)
        {
            var other = obj as VectorTwoMuscariable;
            return this == other;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }


    }

    [Serializable]
    [VariableInfo("Numeric/Structured", "VectorThree", typeof(Vector3), showInMenu: false)]
    [MovedFrom(true, "AtMycelia.Hyphlow",
        "AtMycelia.Amanita.Core",
        "VectorThreeMuscariable")]
    public class VectorThreeMuscariable : Muscariable<Vector3>
    {
        public VectorThreeMuscariable() : base() { }

        public override bool IsArithmeticSupported(SetOperator op) => true;
        public override bool IsRelationalSupported => true;

        public virtual float X
        {
            get => Value.x;
            set
            {
                Vector3 newVal = Value;
                newVal.x = value;
                Value = newVal;
                TriggerOnValueChanged();
            }
        }

        public virtual float Y
        {
            get => Value.y;
            set
            {
                Vector3 newVal = Value;
                newVal.y = value;
                Value = newVal;
                TriggerOnValueChanged();
            }
        }

        public virtual float Z
        {
            get => Value.z;
            set
            {
                Vector3 newVal = Value;
                newVal.z = value;
                Value = newVal;
                TriggerOnValueChanged();
            }
        }

        public static VectorThreeMuscariable operator +(VectorThreeMuscariable a, VectorThreeMuscariable b)
            => new VectorThreeMuscariable { Value = a.Value + b.Value };

        public static VectorThreeMuscariable operator -(VectorThreeMuscariable a, VectorThreeMuscariable b)
            => new VectorThreeMuscariable { Value = a.Value - b.Value };

        public static VectorThreeMuscariable operator +(VectorThreeMuscariable a, VectorTwoMuscariable b)
            => new VectorThreeMuscariable
            {
                Value = new Vector3(a.X + b.X,
                a.Y + b.Y,
                a.Z)
            };

        public static VectorThreeMuscariable operator -(VectorThreeMuscariable a, VectorTwoMuscariable b)
            => new VectorThreeMuscariable
            {
                Value = new Vector3(a.X - b.X,
                a.Y - b.Y,
                a.Z)
            };

        public static VectorThreeMuscariable operator *(VectorThreeMuscariable a, int intVal)
            => new VectorThreeMuscariable { Value = a.Value * intVal };

        public static VectorThreeMuscariable operator *(VectorThreeMuscariable a, float floatVal)
            => new VectorThreeMuscariable { Value = a.Value * floatVal };

        public static VectorThreeMuscariable operator *(VectorThreeMuscariable a, IntMuscariable intVar)
            => new VectorThreeMuscariable { Value = a.Value * intVar.Value };

        public static VectorThreeMuscariable operator *(VectorThreeMuscariable a, FloatMuscariable floatVar)
            => new VectorThreeMuscariable { Value = a.Value * floatVar.Value };

        public static bool operator ==(VectorThreeMuscariable a, VectorThreeMuscariable b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.Value == b.Value;
        }

        public static bool operator !=(VectorThreeMuscariable a, VectorThreeMuscariable b)
        {
            if (ReferenceEquals(a, b)) return false;
            if (!ReferenceEquals(a, null) && !ReferenceEquals(b, null)) return a.Value != b.Value;
            return a.Value == b.Value;
        }

        public override bool Equals(object obj)
        {
            var other = obj as VectorThreeMuscariable;
            return this == other;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }


    }

    [Serializable]
    [VariableInfo("Numeric/Structured", "VectorFour", typeof(Vector4), showInMenu: false)]
    public class VectorFourMuscariable : Muscariable<Vector4>
    {
        public VectorFourMuscariable() : base() { }

        public override bool IsArithmeticSupported(SetOperator op) => true;
        public override bool IsRelationalSupported => true;

        public virtual float X
        {
            get => Value.x;
            set
            {
                Vector4 newVal = Value;
                newVal.x = value;
                Value = newVal;
                TriggerOnValueChanged();
            }
        }

        public virtual float Y
        {
            get => Value.y;
            set
            {
                Vector4 newVal = Value;
                newVal.y = value;
                Value = newVal;
                TriggerOnValueChanged();
            }
        }

        public virtual float Z
        {
            get => Value.z;
            set
            {
                Vector4 newVal = Value;
                newVal.z = value;
                Value = newVal;
                TriggerOnValueChanged();
            }
        }

        public virtual float W
        {
            get => Value.w;
            set
            {
                Vector4 newVal = Value;
                newVal.w = value;
                Value = newVal;
                TriggerOnValueChanged();
            }
        }

        public static VectorFourMuscariable operator +(VectorFourMuscariable a, VectorFourMuscariable b)
            => new VectorFourMuscariable { Value = a.Value + b.Value };

        public static VectorFourMuscariable operator -(VectorFourMuscariable a, VectorFourMuscariable b)
            => new VectorFourMuscariable { Value = a.Value - b.Value };

        public static VectorFourMuscariable operator +(VectorFourMuscariable a, VectorTwoMuscariable b)
            => new VectorFourMuscariable
            {
                Value = new Vector4(a.X + b.X,
                a.Y + b.Y,
                a.Z,
                a.W)
            };

        public static VectorFourMuscariable operator -(VectorFourMuscariable a, VectorTwoMuscariable b)
            => new VectorFourMuscariable
            {
                Value = new Vector4(a.X - b.X,
                a.Y - b.Y,
                a.Z,
                a.W)
            };

        public static VectorFourMuscariable operator *(VectorFourMuscariable a, int intVal)
            => new VectorFourMuscariable { Value = a.Value * intVal };

        public static VectorFourMuscariable operator *(VectorFourMuscariable a, float floatVal)
            => new VectorFourMuscariable { Value = a.Value * floatVal };

        public static VectorFourMuscariable operator *(VectorFourMuscariable a, IntMuscariable intVar)
            => new VectorFourMuscariable { Value = a.Value * intVar.Value };

        public static VectorFourMuscariable operator *(VectorFourMuscariable a, FloatMuscariable floatVar)
            => new VectorFourMuscariable { Value = a.Value * floatVar.Value };

        public static bool operator ==(VectorFourMuscariable a, VectorFourMuscariable b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.Value == b.Value;
        }

        public static bool operator !=(VectorFourMuscariable a, VectorFourMuscariable b)
        {
            if (ReferenceEquals(a, b)) return false;
            if (!ReferenceEquals(a, null) && !ReferenceEquals(b, null)) return a.Value != b.Value;
            return a.Value == b.Value;
        }

        public override bool Equals(object obj)
        {
            var other = obj as VectorFourMuscariable;
            return this == other;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }


    }

    /// <summary>
    /// How many components of a <see cref="VectorMuscariable"/> are in use.
    /// Declared in order of size; code compares them.
    /// </summary>
    public enum VectorDimension
    {
        TwoD,
        ThreeD,
        FourD
    }

    /// <summary>
    /// The one variable type for vectors, meant to replace <see cref="VectorTwoMuscariable"/>,
    /// <see cref="VectorThreeMuscariable"/> and <see cref="VectorFourMuscariable"/>. Always
    /// stores a Vector4; Vector2s and Vector3s assigned to it are padded with zeroes, so the
    /// unused components stay zero. Arithmetic is component-wise.
    ///
    /// <see cref="Dimension"/> is how many components the variable is meant to use. It only
    /// affects how editors present the value, and it grows on its own so that a non-zero
    /// component is never hidden.
    /// </summary>
    [Serializable]
    [VariableInfo("Numeric", "Vector", typeof(Vector4))]
    public class VectorMuscariable : Muscariable<Vector4>
    {
        [SerializeField]
        protected VectorDimension _dimension = VectorDimension.ThreeD;

        public VectorMuscariable() : base() { }

        /// <summary>
        /// Setting a smaller dimension zeroes the components it drops, so the value always
        /// matches what's meant to be in use.
        /// </summary>
        public virtual VectorDimension Dimension
        {
            get => _dimension;
            set
            {
                if (_dimension == value)
                {
                    return;
                }

                _dimension = value;
                if (value < VectorDimension.FourD)
                {
                    _value.w = 0f;
                }
                if (value < VectorDimension.ThreeD)
                {
                    _value.z = 0f;
                }

                TriggerOnValueChanged();
            }
        }

        public static VectorDimension DimensionNeededFor(Vector4 vector)
        {
            if (vector.w != 0f)
            {
                return VectorDimension.FourD;
            }

            return vector.z != 0f ?
                VectorDimension.ThreeD :
                VectorDimension.TwoD;
        }

        public override Vector4 Value
        {
            get => _value;
            set
            {
                GrowDimensionToFit(value);
                base.Value = value;
            }
        }

        // Muscariable<T>.BoxedValue writes straight to _value, bypassing Value
        public override object BoxedValue
        {
            get => base.BoxedValue;
            set
            {
                if (FilterForValueSet(value) is Vector4 vector)
                {
                    GrowDimensionToFit(vector);
                }
                base.BoxedValue = value;
            }
        }

        public override void OnReset()
        {
            GrowDimensionToFit(_startValue);
            base.OnReset();
        }

        private void GrowDimensionToFit(Vector4 vector)
        {
            VectorDimension needed = DimensionNeededFor(vector);
            if (needed > _dimension)
            {
                _dimension = needed;
            }
        }

        public override bool IsArithmeticSupported(SetOperator op) => true;
        public override bool IsComparisonSupported() => true;

        public virtual float X
        {
            get => Value.x;
            set { Vector4 v = Value; v.x = value; Value = v; }
        }

        public virtual float Y
        {
            get => Value.y;
            set { Vector4 v = Value; v.y = value; Value = v; }
        }

        public virtual float Z
        {
            get => Value.z;
            set { Vector4 v = Value; v.z = value; Value = v; }
        }

        public virtual float W
        {
            get => Value.w;
            set { Vector4 v = Value; v.w = value; Value = v; }
        }

        public override void Apply(SetOperator setOperator, Vector4 toApply)
        {
            switch (setOperator)
            {
                case SetOperator.Assign:
                    Value = toApply;
                    break;

                case SetOperator.Add:
                    Value += toApply;
                    break;

                case SetOperator.Subtract:
                    Value -= toApply;
                    break;

                case SetOperator.Multiply:
                    Value = Vector4.Scale(Value, toApply);
                    break;

                case SetOperator.Divide:
                    Vector4 v = Value;
                    Value = new Vector4(v.x / toApply.x, v.y / toApply.y, v.z / toApply.z, v.w / toApply.w);
                    break;

                case SetOperator.Negate:
                    Value = -Value;
                    break;

                default:
                    Debug.LogError($"The {setOperator} set operator is not valid for {ContentType.Name} variable {Key}.");
                    break;
            }
        }

        // The object-taking overloads of Apply/Evaluate convert via ConvertToValue, which can't
        // turn a boxed Vector2/Vector3 into a Vector4, so we pad those ourselves first.
        public override void Apply(SetOperator setOperator, object toApply)
        {
            base.Apply(setOperator, FilterForValueSet(toApply));
        }

        public override bool Evaluate(CompareOperator op, object value)
        {
            return base.Evaluate(op, FilterForValueSet(value));
        }

        protected override bool CanHoldAsValue(object obj)
        {
            return obj is Vector2 || obj is Vector3 || obj is Vector4;
        }

        protected override object FilterForValueSet(object valueToConvert)
        {
            switch (valueToConvert)
            {
                case Vector2 two: return new Vector4(two.x, two.y, 0f, 0f);
                case Vector3 three: return new Vector4(three.x, three.y, three.z, 0f);
                default: return valueToConvert;
            }
        }

        public override Muscariable Clone()
        {
            return new VectorMuscariable
            {
                _scope = _scope,
                _key = _key,
                _itemId = _itemId,
                _dimension = _dimension,
                _startValue = _startValue,
                _value = _value
            };
        }

        public static VectorMuscariable operator +(VectorMuscariable a, VectorMuscariable b)
            => new VectorMuscariable { Value = a.Value + b.Value };

        public static VectorMuscariable operator -(VectorMuscariable a, VectorMuscariable b)
            => new VectorMuscariable { Value = a.Value - b.Value };

        public static VectorMuscariable operator *(VectorMuscariable a, float scalar)
            => new VectorMuscariable { Value = a.Value * scalar };

        public static bool operator ==(VectorMuscariable a, VectorMuscariable b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.Value == b.Value;
        }

        public static bool operator !=(VectorMuscariable a, VectorMuscariable b) => !(a == b);

        public override bool Equals(object obj)
        {
            var other = obj as VectorMuscariable;
            return this == other;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

    }

    [Serializable]
    [VariableInfo("Numeric/Structured", "MatrixFourByFour", typeof(Matrix4x4))]
    public class MatrixFourByFourMuscariable : Muscariable<Matrix4x4>
    {
        public MatrixFourByFourMuscariable() : base() { }
        public override bool IsArithmeticSupported(SetOperator op) => false;
        public override bool IsRelationalSupported => false;
    }
}