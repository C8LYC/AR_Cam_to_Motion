using System.Collections.Generic;

public static class MediaPipeJointMap
{
    static readonly Dictionary<int, JointIndices> s_Map = new Dictionary<int, JointIndices>
    {
        { 0, JointIndices.Nose },
        { 1, JointIndices.LeftEye },
        { 2, JointIndices.LeftEye },
        { 3, JointIndices.LeftEye },
        { 4, JointIndices.RightEye },
        { 5, JointIndices.RightEye },
        { 6, JointIndices.RightEye },
        { 7, JointIndices.LeftEye },
        { 8, JointIndices.RightEye },
        { 9, JointIndices.Jaw },
        { 10, JointIndices.Jaw },
        { 11, JointIndices.LeftShoulder1 },
        { 12, JointIndices.RightShoulder1 },
        { 13, JointIndices.LeftForearm },
        { 14, JointIndices.RightForearm },
        { 15, JointIndices.LeftHand },
        { 16, JointIndices.RightHand },
        { 17, JointIndices.LeftHandPinkyEnd },
        { 18, JointIndices.RightHandPinkyEnd },
        { 19, JointIndices.LeftHandIndexEnd },
        { 20, JointIndices.RightHandIndexEnd },
        { 21, JointIndices.LeftHandThumbEnd },
        { 22, JointIndices.RightHandThumbEnd },
        { 23, JointIndices.LeftUpLeg },
        { 24, JointIndices.RightUpLeg },
        { 25, JointIndices.LeftLeg },
        { 26, JointIndices.RightLeg },
        { 27, JointIndices.LeftFoot },
        { 28, JointIndices.RightFoot },
        { 29, JointIndices.LeftFoot },
        { 30, JointIndices.RightFoot },
        { 31, JointIndices.LeftToes },
        { 32, JointIndices.RightToes },
    };

    public static bool TryGetJoint(int mediaPipeIndex, out JointIndices joint)
    {
        return s_Map.TryGetValue(mediaPipeIndex, out joint);
    }
}
