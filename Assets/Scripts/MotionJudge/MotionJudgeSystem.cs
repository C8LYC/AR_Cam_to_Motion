using UnityEngine;

public class MotionJudgeSystem : MonoBehaviour
{
    public float CalculateAngle(Vector3 start, Vector3 middle, Vector3 end)
    {
        // 建立從中間點（手肘）出發的兩條向量
        Vector3 v1 = start - middle;
        Vector3 v2 = end - middle;

        // 使用 Vector3.Angle 直接取得 0 到 180 度之間的夾角
        float angle = Vector3.Angle(v1, v2);
        return angle;
    }

    private Vector3 lastPosition;
    public float currentVelocity;

    public void TrackVelocity(Vector3 currentPosition)
    {
        // 計算這一幀移動的距離
        float distance = Vector3.Distance(currentPosition, lastPosition);
        
        // 速度 = 距離 / 時間
        currentVelocity = distance / Time.deltaTime;

        // 更新紀錄，供下一幀使用
        lastPosition = currentPosition;
    }


}
