using UnityEngine;

public class game_settings : MonoBehaviour {
	public static game_settings instance;
	
	[Header("回弹参数")]
	[Range(0.01f, 1f)]
	public float snap_speed = 0.15f;  // 回弹速度（越小越慢）
	
	[Range(0.001f, 0.1f)]
	public float snap_distance_threshold = 0.01f;  // 回弹停止距离阈值
	
	[Header("游戏难度")]
	[Range(3, 10)]
	public int ball_color_count = 7;  // 使用的颜色种类数量（3=简单，7=适中，10=困难）
	
	void Awake() {
		// 单例模式
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}
}

