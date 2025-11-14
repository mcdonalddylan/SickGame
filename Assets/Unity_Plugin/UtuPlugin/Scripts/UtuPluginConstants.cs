// Copyright Alex Quevillon. All Rights Reserved.

public class UtuConst {
	public static readonly int INVALID_INT = -999;
	public static readonly string DEFAULT_RESOURCES = "unity default resources";

    public static bool NearlyEquals(float a, float b, float x) {
        return ((a < b) ? (b - a) : (a - b)) <= x;
    }
}
