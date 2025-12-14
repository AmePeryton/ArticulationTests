using System;
using System.Collections.Generic;
using UnityEngine;

// Extension of Mathf, collection of math functions not included in Mathf
public struct MathExt
{
	// Returns modulo between 0 and b, never negative
	// Example: Posmod(370, 360) = 10; Posmod(-10, 360) = 350;
	// Counting order is reversed on negative b value
	public static float PosMod(float a, float b)
	{
		if (b == 0)
		{
			throw new DivideByZeroException();
		}
		if (b < 0)
		{
			return -((a % b + b) % b);
		}
		else
		{
			return (a % b + b) % b;
		}
	}

	// Removes y component from a Vector3 (normal implicit conversion removes the z component instead)
	public static Vector2 Flatten(Vector3 a)
	{
		return new Vector2(a.x, a.z);
	}

	// Sets y component of a Vector3 to a constant (defaults to 0)
	// Equivelent to Unflatten(Flatten(a), y);
	public static Vector3 Flatten3D(Vector3 a, float y = 0)
	{
		return new Vector3(a.x, y, a.z);
	}

	// Convert flattened Vector2 back into a Vector3 with a constant y component (defaults to 0)
	public static Vector3 Unflatten(Vector2 a, float y = 0)
	{
		return new Vector3(a.x, y, a.y);
	}

	// Returns the angle of the direction from a to b in degrees
	// Angle returned is always between 0 and 360
	public static float DirectionAngle(Vector2 a, Vector2 b)
	{
		return PosMod(Mathf.Atan2(b.x - a.x, b.y - a.y) * Mathf.Rad2Deg, 360f);
	}

	// Returns difference between 2 angles on the same axis in degrees of the smallest angle between them
	// Correctly accounts for wrapping from 360 to 0 and 180 to -180, etc.
	// Example: AngleDifference(90, 30) = 60; AngleDifference(20, 350) = 30; AngleDifference(350, 20) = -30;
	// Inputs will automatically be converted to 0-360 degree space, and the output will be -180 to 180
	public static float AngleDifference(float a, float b)
	{
		return -PosMod(a - b - 180, 360) + 180;
	}

	public static float Remap(float input, float inMin, float inMax, float outMin, float outMax)
	{
		if (inMax == inMin || outMax == outMin)
		{
			throw new DivideByZeroException();
		}

		return ((input - inMin) / (inMax - inMin)) * (outMax - outMin) + outMin;
	}

	// Vector2 version, if preferred
	public static float Remap(float input, Vector2 inLimits, Vector2 outLimits)
	{
		if (inLimits[0] == inLimits[1] || outLimits[0] == outLimits[1])
		{
			throw new DivideByZeroException();
		}

		return ((input - inLimits[0]) / (inLimits[1] - inLimits[0])) * (outLimits[1] - outLimits[0]) + outLimits[0];
	}

	public static string ToPercentage(float input, int precision = 2)
	{
		string format = "F" + precision;
		string output = (input * 100).ToString(format) + "%";
		return output;
	}

	public static List<float> ToList(Vector2 input)
	{
		return new List<float>() { input.x, input.y};
	}

	public static List<float> ToList(Vector3 input)
	{
		return new List<float>() { input.x, input.y, input.z };
	}

	public static float RoundFloat(float input, float increment)
	{
		return MathF.Round(input / increment) * increment;
	}

	public static Vector3 RoundVector3(Vector3 input, float increment)
	{
		return new Vector3(
			MathF.Round(input.x / increment) * increment,
			MathF.Round(input.y / increment) * increment,
			MathF.Round(input.z / increment) * increment);
	}

	public static Vector3 RoundVector3(Vector3 input, Vector3 increments)
	{
		return new Vector3(
			MathF.Round(input.x / increments.x) * increments.x,
			MathF.Round(input.y / increments.y) * increments.y,
			MathF.Round(input.z / increments.z) * increments.z);
	}

	// Checks if a point is on the given plane with an option for allowed distance
	public static bool IsPointOnPlane(Plane plane, Vector3 point, float allowance = 0)
	{
		// If the point is within the allowed distance of the plane, return true
		if (plane.GetDistanceToPoint(point) <= allowance)
		{
			return true;
		}
		// Otherwise, return false
		return false;
	}

	// Alternate IsPointOnPlane method to take 2 vectors in place of a plane
	public static bool IsPointOnPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 point, float allowance = 0)
	{
		// Return using original method
		return IsPointOnPlane(new Plane(planeNormal, planePoint), point, allowance);
	}

	// Checks if an euler rotation is oriented on the given plane with an option for angle allowance (degrees)
	public static bool IsRotationOnPlane(Vector3 planeNormal, Vector3 rotation, float allowance = 0)
	{
		// Get angle between plane normal and the rotated forward vector
		float af = Vector3.Angle(planeNormal, Quaternion.Euler(rotation) * Vector3.forward);
		// Get angle between plane normal and the rotated up vector
		float au = Vector3.Angle(planeNormal, Quaternion.Euler(rotation) * Vector3.up);

		// If the rotation is within the allowed angle of the plane, return true
		if (af >= 90 - allowance && af <= 90 + allowance && au >= 90 - allowance && au <= 90 + allowance)
		{
			return true;
		}
		// Otherwise, return false
		return false;
	}

	// Alternate IsRotationOnPlane method to take a plane in place of a normal vector
	public static bool IsRotationOnPlane(Plane plane, Vector3 rotation, float allowance = 0)
	{
		// Return using original method
		return IsRotationOnPlane(plane.normal, rotation, allowance);
	}

	// Checks if a point is on the given axis with an option for allowed distance
	public static bool IsPointOnAxis(Vector3 axisDirection, Vector3 axisPoint, Vector3 point, float allowance = 0)
	{
		// If the point is within the allowed distance of the axis, return true
		if (Vector3.Cross(axisDirection.normalized, point - axisPoint).magnitude <= allowance)
		{
			return true;
		}
		// Otherwise, return false
		return false;
	}

	// Checks if an euler rotation is oriented on the given axis with an option for angle allowance (degrees)
	public static bool IsRotationOnAxis(Vector3 axisDirection, Vector3 rotation, float allowance = 0)
	{
		// Get angle between directions
		float a = Vector3.Angle(axisDirection, Quaternion.Euler(rotation) * Vector3.forward);

		// If the rotation is within the allowed angle of the axis, return true
		if ((a >= - allowance && a <= allowance) || a >= 180 - allowance)
		{
			return true;
		}
		// Otherwise, return false
		return false;
	}

	// Aligns the given point to the closest position on the plane
	public static Vector3 AlignPointToPlane(Plane plane, Vector3 point)
	{
		// Very simple for now, but I may add more stuff later
		return plane.ClosestPointOnPlane(point);
	}

	// Alternate AlignPointToPlane method to take 2 vectors in place of a plane
	public static Vector3 AlignPointToPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
	{
		return AlignPointToPlane(new Plane(planeNormal, planePoint), point);
	}

	// Aligns the euler rotation to the plane
	public static Vector3 AlignRotationToPlane(Vector3 planeNormal, Vector3 rotation)
	{
		Plane p0 = new(planeNormal, Vector3.zero);
		Quaternion q = Quaternion.Euler(rotation);

		Vector3 newFwd = p0.ClosestPointOnPlane(q * Vector3.forward).normalized;
		if (newFwd.magnitude == 0)
		{
			// If part's forward vector is orthogonal to the plane, use the right vector instead
			newFwd = p0.ClosestPointOnPlane(q * Vector3.right).normalized;
		}
		// Get the new up vector in local space
		Vector3 newUp = p0.ClosestPointOnPlane(q * Vector3.up).normalized;
		if (newUp.magnitude == 0)
		{
			// If part's up vector is orthogonal to the plane, use the right vector instead
			newUp = p0.ClosestPointOnPlane(q * Vector3.right).normalized;
			// NOTE: Since forward and up are orthogonal, and only 1 can be parallel to the plane's normal
			// Only one of them can be orthogonal to the plane, and get defaulted to the right vector
			// NEVER BOTH AT THE SAME TIME
		}

		// Set rotation to be aligned with plane
		return Quaternion.LookRotation(newFwd, newUp).eulerAngles;
	}

	// Alternate AlignRotationToPlane method to take a plane in place of a normal vector
	public static Vector3 AlignRotationToPlane(Plane plane, Vector3 rotation)
	{
		return AlignRotationToPlane(plane.normal, rotation);
	}

	// Aligns the given point to the closest position on the axis
	public static Vector3 AlignPointToAxis(Vector3 axisDirection, Vector3 axisPoint, Vector3 point)
	{
		// Set point to the closest point on the axis
		return Vector3.Project(point - axisPoint, axisDirection) + axisPoint;
	}

	// Aligns the euler rotation to the axis
	public static Vector3 AlignRotationToAxis(Vector3 axisDirection, Vector3 rotation)
	{
		// Check that the rotation is on the axis
		Quaternion q = Quaternion.Euler(rotation);
		// Align forward direction with or opposite of the axis direction
		Vector3 newFwd = axisDirection;
		if (Vector3.Dot(q * Vector3.forward, axisDirection) < 0)
		{
			newFwd = -newFwd;
		}
		Vector3 newRotation = Quaternion.LookRotation(newFwd).eulerAngles;
		// Keep the old Z rotation, since it has no bearing on symmetry,
		// and the LookRotation method could mess it up since the up direction is unspecified
		newRotation.z = rotation.z;

		// Set rotation to be aligned with axis
		return newRotation;
	}

	// 3D Perlin Noise
	public static float Perlin3D(Vector3 point, float frequency)
	{
		point *= frequency;

		int flooredPointX0 = Mathf.FloorToInt(point.x);
		int flooredPointY0 = Mathf.FloorToInt(point.y);
		int flooredPointZ0 = Mathf.FloorToInt(point.z);

		float distanceX0 = point.x - flooredPointX0;
		float distanceY0 = point.y - flooredPointY0;
		float distanceZ0 = point.z - flooredPointZ0;

		float distanceX1 = distanceX0 - 1f;
		float distanceY1 = distanceY0 - 1f;
		float distanceZ1 = distanceZ0 - 1f;

		flooredPointX0 &= permutationCount;
		flooredPointY0 &= permutationCount;
		flooredPointZ0 &= permutationCount;

		int flooredPointX1 = flooredPointX0 + 1;
		int flooredPointY1 = flooredPointY0 + 1;
		int flooredPointZ1 = flooredPointZ0 + 1;

		int permutationX0 = permutation[flooredPointX0];
		int permutationX1 = permutation[flooredPointX1];

		int permutationY00 = permutation[permutationX0 + flooredPointY0];
		int permutationY10 = permutation[permutationX1 + flooredPointY0];
		int permutationY01 = permutation[permutationX0 + flooredPointY1];
		int permutationY11 = permutation[permutationX1 + flooredPointY1];

		Vector3 direction000 = directions[permutation[permutationY00 + flooredPointZ0] & directionCount];
		Vector3 direction100 = directions[permutation[permutationY10 + flooredPointZ0] & directionCount];
		Vector3 direction010 = directions[permutation[permutationY01 + flooredPointZ0] & directionCount];
		Vector3 direction110 = directions[permutation[permutationY11 + flooredPointZ0] & directionCount];
		Vector3 direction001 = directions[permutation[permutationY00 + flooredPointZ1] & directionCount];
		Vector3 direction101 = directions[permutation[permutationY10 + flooredPointZ1] & directionCount];
		Vector3 direction011 = directions[permutation[permutationY01 + flooredPointZ1] & directionCount];
		Vector3 direction111 = directions[permutation[permutationY11 + flooredPointZ1] & directionCount];

		float value000 = Scalar(direction000, new Vector3(distanceX0, distanceY0, distanceZ0));
		float value100 = Scalar(direction100, new Vector3(distanceX1, distanceY0, distanceZ0));
		float value010 = Scalar(direction010, new Vector3(distanceX0, distanceY1, distanceZ0));
		float value110 = Scalar(direction110, new Vector3(distanceX1, distanceY1, distanceZ0));
		float value001 = Scalar(direction001, new Vector3(distanceX0, distanceY0, distanceZ1));
		float value101 = Scalar(direction101, new Vector3(distanceX1, distanceY0, distanceZ1));
		float value011 = Scalar(direction011, new Vector3(distanceX0, distanceY1, distanceZ1));
		float value111 = Scalar(direction111, new Vector3(distanceX1, distanceY1, distanceZ1));

		float smoothDistanceX = SmoothDistance(distanceX0);
		float smoothDistanceY = SmoothDistance(distanceY0);
		float smoothDistanceZ = SmoothDistance(distanceZ0);

		return Mathf.Lerp(
			Mathf.Lerp(Mathf.Lerp(value000, value100, smoothDistanceX), Mathf.Lerp(value010, value110, smoothDistanceX), smoothDistanceY),
			Mathf.Lerp(Mathf.Lerp(value001, value101, smoothDistanceX), Mathf.Lerp(value011, value111, smoothDistanceX), smoothDistanceY),
			smoothDistanceZ);
	}

	// Perlin helpers
	private const int permutationCount = 255;
	private const int directionCount = 15;
	private static readonly int[] permutation = {
		151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225,
		140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148,
		247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32,
		57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175,
		74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122,
		60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54,
		65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169,
		200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64,
		52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212,
		207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213,
		119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9,
		129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104,
		218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241,
		81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157,
		184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93,
		222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180,

		151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225,
		140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148,
		247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32,
		57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175,
		74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122,
		60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54,
		65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169,
		200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64,
		52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212,
		207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213,
		119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9,
		129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104,
		218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241,
		81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157,
		184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93,
		222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180
	};

	private static readonly Vector3[] directions = {
		new( 1f, 1f, 0f),
		new(-1f, 1f, 0f),
		new( 1f,-1f, 0f),
		new(-1f,-1f, 0f),
		new( 1f, 0f, 1f),
		new(-1f, 0f, 1f),
		new( 1f, 0f,-1f),
		new(-1f, 0f,-1f),
		new( 0f, 1f, 1f),
		new( 0f,-1f, 1f),
		new( 0f, 1f,-1f),
		new( 0f,-1f,-1f),

		new( 1f, 1f, 0f),
		new(-1f, 1f, 0f),
		new( 0f,-1f, 1f),
		new( 0f,-1f,-1f)
	};

	private static float Scalar(Vector3 a, Vector3 b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}

	private static float SmoothDistance(float d)
	{
		return d * d * d * (d * (d * 6f - 15f) + 10f);
	}
}
