using UnityEngine;
using Fungus;
using System.Reflection;

public class BubbleFollowSpeaker : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 2.5f, 0);
    private Transform speakerTransform;
    private SayDialog sayDialog;

    // 缓存 FieldInfo 以提高性能
    private static FieldInfo speakingCharacterField;

    void Start()
    {
        // 获取 SayDialog 实例（全局唯一）
        sayDialog = SayDialog.GetSayDialog();
        if (sayDialog == null)
        {
            Debug.LogWarning("BubbleFollowSpeaker: 未找到 SayDialog，请确保场景中有 Flowchart。");
        }

        // 如果还没缓存 FieldInfo，则初始化
        if (speakingCharacterField == null)
        {
            // 获取 SayDialog 类中的静态私有字段 "speakingCharacter"
            speakingCharacterField = typeof(SayDialog).GetField("speakingCharacter",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (speakingCharacterField == null)
            {
                Debug.LogError("BubbleFollowSpeaker: 无法找到 speakingCharacter 字段，请检查 Fungus 版本。");
            }
        }
    }

    void Update()
    {
        // 从静态字段中读取当前说话的角色
        if (speakingCharacterField != null)
        {
            Character currentSpeaker = speakingCharacterField.GetValue(null) as Character;
            if (currentSpeaker != null)
            {
                speakerTransform = currentSpeaker.transform;
            }
        }

        // 更新气泡位置
        if (speakerTransform != null)
        {
            transform.position = speakerTransform.position + offset;
        }
    }
}