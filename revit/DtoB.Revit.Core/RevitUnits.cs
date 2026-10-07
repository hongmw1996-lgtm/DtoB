using DtoB.Core;

namespace DtoB.Revit.Core;

public static class RevitUnits
{
    public const double MillimetersPerFoot = 304.8;
    public static double ToInternalFeet(double millimeters) { Contract.Finite(millimeters); return millimeters / MillimetersPerFoot; }
    public static double FromInternalFeet(double feet) { Contract.Finite(feet); var mm = feet * MillimetersPerFoot; Contract.Finite(mm); return mm; }
}
