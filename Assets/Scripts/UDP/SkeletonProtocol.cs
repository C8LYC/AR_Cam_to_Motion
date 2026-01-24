using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

using UnityEngine.XR.ARFoundation.Samples;

[Serializable]
public class SkeletonProtocol
{
    // 全身 91 點與辨識用 14 點 (13點 + 補上1號Hip點)
    public const int JointCount = 91;
    public const int ReducedJointCount = 14; 

    // Position (12 bytes) + Rotation (16 bytes) = 28 bytes per joint
    private const int BytesPerJoint = 28; 

    [Serializable]
    public struct JointData
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    // 根據您的需求對應的 Unity 關節索引
    // 包含：51(頭), 19,63(肩), 21,65(肘), 22,66(腕), 1(腰), 2,7(髖), 3,8(膝), 4,9(踝)
    public static readonly int[] ReducedIndices = new int[ReducedJointCount] 
    {
        51, 19, 63, 21, 65, 22, 66, 1, 2, 7, 3, 8, 4, 9
    };

    #region 關鍵點模式 (Reduced 14 點)
    public static byte[] PackReduced(ARHumanBody body)
    {
		var joints = body.joints;
        JointData[] reducedData = new JointData[ReducedJointCount];
        
        for (int i = 0; i < ReducedJointCount; i++)
        {
            int unityIndex = ReducedIndices[i];
            if (unityIndex < joints.Length)
            {
                reducedData[i].position = joints[unityIndex].localPose.position;
                reducedData[i].rotation = joints[unityIndex].localPose.rotation;
            }
        }
        return Serialize(reducedData, ReducedJointCount);
    }

    public static byte[] PackReduced(ARHumanBody body, Transform[] boneTransforms)
    {
		var joints = body.joints;
        JointData[] reducedData = new JointData[ReducedJointCount];
        
        for (int i = 0; i < ReducedJointCount; i++)
        {
            int unityIndex = ReducedIndices[i];
            if (unityIndex < joints.Length)
            {
                if(joints[unityIndex].tracked)
                {
					reducedData[i].position = joints[unityIndex].localPose.position;
					reducedData[i].rotation = joints[unityIndex].localPose.rotation;
				}
				else
				{
					// 使用骨架預設位置與旋轉
					reducedData[i].position = boneTransforms[unityIndex].localPosition;
					reducedData[i].rotation = boneTransforms[unityIndex].localRotation;
				}
			}
            
        }
        return Serialize(reducedData, ReducedJointCount);
    }
    #endregion

    #region 全傳送模式 (91 點)
    public static byte[] PackFull(ARHumanBody body)
    {
        var joints = body.joints;
        JointData[] fullData = new JointData[JointCount];
        for (int i = 0; i < JointCount; i++)
        {
            if(i == 0)
            {
                fullData[i].position = joints[i].anchorPose.position;
				fullData[i].rotation = joints[i].anchorPose.rotation;
            }
			else if (i < joints.Length)
			{
				fullData[i].position = joints[i].localPose.position;
				fullData[i].rotation = joints[i].localPose.rotation;
			}
		}
        return Serialize(fullData, JointCount);
    }

	public static byte[] PackFull(ARHumanBody body, BoneController boneController)
	{
		var joints = body.joints;
		JointData[] fullData = new JointData[JointCount];
		for (int i = 0; i < JointCount; i++)
		{
            if(i == 0)
            {
                //fullData[i].position = boneController.transform.position;
				//fullData[i].rotation = boneController.transform.rotation;

                fullData[i].position = body.pose.position;
				fullData[i].rotation = body.pose.rotation;
                
                //fullData[i].position = joints[i].anchorPose.position;
				//fullData[i].rotation = joints[i].anchorPose.rotation;
            }
			else if (i < joints.Length)
			{
				if (joints[i].tracked)
				{
					fullData[i].position = joints[i].localPose.position;
					fullData[i].rotation = joints[i].localPose.rotation;
				}
				else
				{
					fullData[i].position = boneController.m_BoneMapping[i].localPosition;
					fullData[i].rotation = boneController.m_BoneMapping[i].localRotation;
				}
			}
		}
		return Serialize(fullData, JointCount);
	}
	#endregion

	private static byte[] Serialize(JointData[] joints, int count)
    {
		int packetSize = 4 + (count * BytesPerJoint);
        byte[] packet = new byte[packetSize];
        Buffer.BlockCopy(BitConverter.GetBytes(count), 0, packet, 0, 4);

        for (int i = 0; i < count; i++)
        {
            int offset = 4 + (i * BytesPerJoint);
            // Position
            Buffer.BlockCopy(BitConverter.GetBytes(joints[i].position.x), 0, packet, offset, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(joints[i].position.y), 0, packet, offset + 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(joints[i].position.z), 0, packet, offset + 8, 4);
            // Rotation
            Buffer.BlockCopy(BitConverter.GetBytes(joints[i].rotation.x), 0, packet, offset + 12, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(joints[i].rotation.y), 0, packet, offset + 16, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(joints[i].rotation.z), 0, packet, offset + 20, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(joints[i].rotation.w), 0, packet, offset + 24, 4);
        }
        return packet;
    }

    public static JointData[] UnpackFromProtocol(byte[] data)
    {
        if (data.Length < 4) return null;

        // 讀取前 4 bytes 取得點數 (int)
        int count = BitConverter.ToInt32(data, 0);
        SkeletonProtocol.JointData[] joints = new SkeletonProtocol.JointData[count];
        
        int bytesPerJoint = 28; // 3 floats (pos) + 4 floats (rot)

        for (int i = 0; i < count; i++)
        {
            int offset = 4 + (i * bytesPerJoint);
            
            // Position
            joints[i].position.x = BitConverter.ToSingle(data, offset);
            joints[i].position.y = BitConverter.ToSingle(data, offset + 4);
            joints[i].position.z = BitConverter.ToSingle(data, offset + 8);

            // Rotation
            joints[i].rotation.x = BitConverter.ToSingle(data, offset + 12);
            joints[i].rotation.y = BitConverter.ToSingle(data, offset + 16);
            joints[i].rotation.z = BitConverter.ToSingle(data, offset + 20);
            joints[i].rotation.w = BitConverter.ToSingle(data, offset + 24);
        }
        return joints;
    }
}