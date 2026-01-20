namespace XisfSharp.Properties;

public enum XisfPropertyType
{
    // Scalars
    Boolean,
    Int8, UInt8,
    Int16, UInt16,
    Int32, UInt32,
    Int64, UInt64,
    Int128, UInt128,
    Float32, Float64, Float128,

    // Other atomics
    String,
    TimePoint,

    // Complex
    Complex32, Complex64, Complex128,

    // Structured
    I8Vector, UI8Vector,
    I16Vector, UI16Vector,
    I32Vector, UI32Vector,
    I64Vector, UI64Vector,
    I128Vector, UI128Vector,
    F32Vector, F64Vector, F128Vector,
    C32Vector, C64Vector, C128Vector,

    I8Matrix, UI8Matrix,
    I16Matrix, UI16Matrix,
    I32Matrix, UI32Matrix,
    I64Matrix, UI64Matrix,
    I128Matrix, UI128Matrix,
    F32Matrix, F64Matrix, F128Matrix,
    C32Matrix, C64Matrix, C128Matrix,

    Table,
}
