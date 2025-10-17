using UnityEngine;
using System.Collections;

public class ball : MonoBehaviour {
	public int color_index;
	public int grid_x, grid_y, ball_index;
	SpriteRenderer spr_this;
	public bool is_moving = false;
	Vector3 ball_position_when_clicked = Vector3.zero;
	
	// 吸附相关
	private bool is_snapping = false;
	private Vector3 snap_target_position;
	
	// 环绕影子副本
	private GameObject shadow_clone = null;
	private Vector3 shadow_snap_target = Vector3.zero;  // 影子的回弹目标位置

	#region hidden_stuff
	public void create_ball(int x, int y, int index) {
		//parameters
		grid_x = x;
		grid_y = y;
		ball_index = index;

		//sprite setup
		spr_this = gameObject.AddComponent<SpriteRenderer>();
		
		// ✅ 根据 game_settings 中设定的颜色数量随机
		int max_colors = ball_sprite_ref.object_reference.ball_sprite.Length;
		int use_colors = max_colors;  // 默认使用全部颜色
		
		// 如果有 game_settings，使用其配置的颜色数量
		if (game_settings.instance != null) {
			use_colors = Mathf.Min(game_settings.instance.ball_color_count, max_colors);
		}
		
		color_index = Random.Range(0, use_colors);
		spr_this.sprite = ball_sprite_ref.object_reference.ball_sprite[color_index];

		//components
		gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
		gameObject.tag = "ball";

	}

	public static void store_current_positions_off_all_balls() {
		foreach (GameObject ball in game_control.all_balls()) {
			game_control.get_ball(ball).ball_position_when_clicked = ball.transform.position;
		}
	}

	void stop_dragging_balls() {
		// ⭐ 新逻辑：检测匹配，决定是吸附还是回退
		
		// 1. 计算移动了多少格
		float distance = 0;
		if (game_control.moving_direction == game_control.direction.horizontal) {
			distance = game_control.drag_offset.x;
		} else if (game_control.moving_direction == game_control.direction.vertical) {
			distance = game_control.drag_offset.y;
		}
		
		int steps = Mathf.RoundToInt(distance / game_control._grid_spacing);
		
		if (steps == 0) {
			// 没有移动足够距离，回退到原位
			snap_all_balls_back_to_original();
		} else {
			// 2. 记录移动的行/列索引
			int moved_line_index = 0;
			if (game_control.moving_direction == game_control.direction.horizontal) {
				moved_line_index = game_control.get_ball(game_control.clicked_ball).grid_y;
			} else if (game_control.moving_direction == game_control.direction.vertical) {
				moved_line_index = game_control.get_ball(game_control.clicked_ball).grid_x;
			}
			
			// 3. 临时应用移动（更新网格逻辑）
			if (game_control.moving_direction == game_control.direction.horizontal) {
				shift_row(moved_line_index, steps);
			} else if (game_control.moving_direction == game_control.direction.vertical) {
				shift_column(moved_line_index, steps);
			}
			
			// 4. 检测是否有匹配
			bool has_match = check_line_for_matches(game_control.moving_direction, moved_line_index);
			
			if (has_match) {
				// 5a. 有匹配：吸附到新位置
				snap_all_balls_to_new_position();
			} else {
				// 5b. 无匹配：撤销移动，回退到原位置
				if (game_control.moving_direction == game_control.direction.horizontal) {
					shift_row(moved_line_index, -steps);
				} else if (game_control.moving_direction == game_control.direction.vertical) {
					shift_column(moved_line_index, -steps);
				}
				snap_all_balls_back_to_original();
			}
		}
		
		//resetuj parametre
		game_control.moving_direction = game_control.direction.none;
		game_control.dragging_balls_active = false;
		game_control.mouse_position_when_clicked = Vector3.zero;

		//resetuj sacuvane pozicije
		foreach (GameObject ball in game_control.all_balls()) {
			game_control.get_ball(ball).ball_position_when_clicked = Vector3.zero;
		}

		//resetuj direction vektor
		game_control.direction_to_move_balls = Vector3.one;
	}
	
	// 清理所有影子副本
	void destroy_all_shadow_clones() {
		foreach (GameObject ball in game_control.all_balls()) {
			ball ball_component = game_control.get_ball(ball);
			if (ball_component.shadow_clone != null) {
				Destroy(ball_component.shadow_clone);
				ball_component.shadow_clone = null;
			}
		}
	}
	
	// 同步更新影子副本位置
	void update_shadow_clone_position(GameObject ball_obj) {
		ball ball_component = game_control.get_ball(ball_obj);
		
		if (ball_component.shadow_clone != null) {
			// 计算影子偏移（根据拖动方向动态计算）
			Vector3 shadow_offset = Vector3.zero;
			
			if (game_control.moving_direction == game_control.direction.horizontal) {
				float grid_width = game_control._grid_width * game_control._grid_spacing;
				// 根据拖动方向决定影子在左边还是右边
				if (game_control.drag_offset.x > 0) {
					// 向右拖 → 影子在左边
					shadow_offset = new Vector3(-grid_width, 0, 0);
				} else {
					// 向左拖 → 影子在右边
					shadow_offset = new Vector3(grid_width, 0, 0);
				}
			} else if (game_control.moving_direction == game_control.direction.vertical) {
				float grid_height = game_control._grid_height * game_control._grid_spacing;
				// 根据拖动方向决定影子在上边还是下边
				if (game_control.drag_offset.y > 0) {
					// 向上拖 → 影子在下边
					shadow_offset = new Vector3(0, -grid_height, 0);
				} else {
					// 向下拖 → 影子在上边
					shadow_offset = new Vector3(0, grid_height, 0);
				}
			}
			
			ball_component.shadow_clone.transform.position = ball_obj.transform.position + shadow_offset;
		}
	}
	
	// 环绕移动一行
	void shift_row(int row, int steps) {
		int width = game_control._grid_width;
		
		// 标准化步数（处理负数和超过宽度的情况）
		steps = ((steps % width) + width) % width;
		
		if (steps == 0) return;

		// 临时保存这一行
		GameObject[] temp = new GameObject[width];
		for (int x = 0; x < width; x++) {
			temp[x] = game_control.grid[x, row];
		}

		// 环绕移动
		for (int x = 0; x < width; x++) {
			int new_x = (x + steps) % width;
			game_control.grid[new_x, row] = temp[x];
			game_control.get_ball(temp[x]).grid_x = new_x;
		}
	}

	// 环绕移动一列
	void shift_column(int col, int steps) {
		int height = game_control._grid_height;
		
		// 标准化步数
		steps = ((steps % height) + height) % height;
		
		if (steps == 0) return;

		// 临时保存这一列
		GameObject[] temp = new GameObject[height];
		for (int y = 0; y < height; y++) {
			temp[y] = game_control.grid[col, y];
		}

		// 环绕移动
		for (int y = 0; y < height; y++) {
			int new_y = (y + steps) % height;
			game_control.grid[col, new_y] = temp[y];
			game_control.get_ball(temp[y]).grid_y = new_y;
		}
	}
	
	// 检查指定行/列是否有3个或以上连续同色球
	bool check_line_for_matches(game_control.direction direction, int line_index) {
		if (direction == game_control.direction.horizontal) {
			return check_row_matches(line_index);
		} else if (direction == game_control.direction.vertical) {
			return check_column_matches(line_index);
		}
		return false;
	}
	
	// 检查一行是否有3个或以上连续同色
	bool check_row_matches(int row) {
		int width = game_control._grid_width;
		
		for (int start_x = 0; start_x < width; start_x++) {
			GameObject first_ball = game_control.grid[start_x, row];
			if (first_ball == null) continue;
			
			int match_color = game_control.get_ball(first_ball).color_index;
			int match_count = 1;
			
			// 向右数连续同色的球
			for (int x = start_x + 1; x < width; x++) {
				GameObject current_ball = game_control.grid[x, row];
				if (current_ball == null) break;
				
				if (game_control.get_ball(current_ball).color_index == match_color) {
					match_count++;
				} else {
					break;
				}
			}
			
			if (match_count >= 3) {
				return true;
			}
		}
		
		return false;
	}
	
	// 检查一列是否有3个或以上连续同色
	bool check_column_matches(int col) {
		int height = game_control._grid_height;
		
		for (int start_y = 0; start_y < height; start_y++) {
			GameObject first_ball = game_control.grid[col, start_y];
			if (first_ball == null) continue;
			
			int match_color = game_control.get_ball(first_ball).color_index;
			int match_count = 1;
			
			// 向上数连续同色的球
			for (int y = start_y + 1; y < height; y++) {
				GameObject current_ball = game_control.grid[col, y];
				if (current_ball == null) break;
				
				if (game_control.get_ball(current_ball).color_index == match_color) {
					match_count++;
				} else {
					break;
				}
			}
			
			if (match_count >= 3) {
				return true;
			}
		}
		
		return false;
	}
	
	// 让所有移动的球吸附到新位置（有匹配时）
	void snap_all_balls_to_new_position() {
		foreach (GameObject ball in game_control.all_balls()) {
			if (game_control.get_ball(ball).is_moving) {
				ball ball_component = game_control.get_ball(ball);
				
				// 目标位置 = 更新后的网格位置
				Vector3 target_pos = new Vector3(
					ball_component.grid_x * game_control._grid_spacing,
					ball_component.grid_y * game_control._grid_spacing,
					0
				) + game_control._grid_spawn_transform.position;
				
				ball_component.snap_target_position = target_pos;
				ball_component.is_snapping = true;
				
				// ⭐ 如果有影子，计算影子的回弹目标位置
				if (ball_component.shadow_clone != null) {
					Vector3 shadow_offset = ball_component.shadow_clone.transform.position - ball.transform.position;
					ball_component.shadow_snap_target = target_pos + shadow_offset;
				}
			}
		}
	}

	// 让所有移动的球回退到原始网格位置
	void snap_all_balls_back_to_original() {
		foreach (GameObject ball in game_control.all_balls()) {
			if (game_control.get_ball(ball).is_moving) {
				ball ball_component = game_control.get_ball(ball);
				
				// 目标位置 = 原始网格位置
				Vector3 target_pos = new Vector3(
					ball_component.grid_x * game_control._grid_spacing,
					ball_component.grid_y * game_control._grid_spacing,
					0
				) + game_control._grid_spawn_transform.position;
				
				ball_component.snap_target_position = target_pos;
				ball_component.is_snapping = true;
				
				// ⭐ 如果有影子，计算影子的回弹目标位置
				if (ball_component.shadow_clone != null) {
					// 计算影子当前的偏移
					Vector3 shadow_offset = ball_component.shadow_clone.transform.position - ball.transform.position;
					// 影子的目标 = 真实球的目标 + 当前偏移
					ball_component.shadow_snap_target = target_pos + shadow_offset;
				}
			}
		}
	}

	void OnMouseUp() {
		stop_dragging_balls();
		game_control.clicked_ball = null;
	}
	#endregion

	void OnMouseDown() {
		game_control.clicked_ball = gameObject;
		game_control.store_mouse_position_when_clicked();

		if (game_control.moving_direction == game_control.direction.none) {
			game_control.calculating_direction_active = true;
		}
	}


	void start_dragging_balls(int direction) {
		game_control.dragging_balls_active = true;
		ball.store_current_positions_off_all_balls();
		set_line_movable(game_control.clicked_ball, direction);
		
		// 为所有可移动的球创建影子副本
		create_shadow_clones_for_moving_balls(direction);
	}
	
	// 为所有可移动的球创建影子副本（首尾相连效果）
	void create_shadow_clones_for_moving_balls(int direction) {
		foreach (GameObject ball_obj in game_control.all_balls()) {
			if (game_control.get_ball(ball_obj).is_moving) {
				ball ball_component = game_control.get_ball(ball_obj);
				
				// 创建影子副本
				GameObject shadow = new GameObject("shadow_" + ball_obj.name);
				SpriteRenderer shadow_spr = shadow.AddComponent<SpriteRenderer>();
				shadow_spr.sprite = ball_component.spr_this.sprite;
				shadow_spr.sortingOrder = ball_component.spr_this.sortingOrder;
				
				// 设置父物体为影子容器
				shadow.transform.SetParent(game_control.shadows_container.transform);
				
				// 计算影子位置（在环绕的另一端）
				Vector3 shadow_offset = Vector3.zero;
				if (direction == 0) {
					// 横向：在网格宽度的另一端
					float grid_width = game_control._grid_width * game_control._grid_spacing;
					shadow_offset = new Vector3(-grid_width, 0, 0);  // 默认在左边
				} else if (direction == 1) {
					// 纵向：在网格高度的另一端
					float grid_height = game_control._grid_height * game_control._grid_spacing;
					shadow_offset = new Vector3(0, -grid_height, 0);  // 默认在下边
				}
				
				shadow.transform.position = ball_obj.transform.position + shadow_offset;
				ball_component.shadow_clone = shadow;
			}
		}
	}

	void set_line_movable(GameObject compared_ball, int line_type) {
		//ukljucivanje is movable parametra
		foreach (GameObject current_ball in game_control.all_balls()) {
			//0 = row
			if (line_type == 0) {
				if (current_ball.GetComponent<ball>().grid_y == compared_ball.GetComponent<ball>().grid_y) {
					current_ball.GetComponent<ball>().is_moving = true;
				}
			}
			//1 = col
			if (line_type == 1) {
				if (current_ball.GetComponent<ball>().grid_x == compared_ball.GetComponent<ball>().grid_x) {
					current_ball.GetComponent<ball>().is_moving = true;
				}
			}
		}

		//namestanje pravca kretanja
		//row
		if (line_type == 0) {
			game_control.direction_to_move_balls = new Vector3(1, 0, 0);	//da se pomera samo po x osi
		}
		//col
		if (line_type == 1) {
			game_control.direction_to_move_balls = new Vector3(0, 1, 0);	//da se pomera samo po y osi
		}
	}

	void Update() {

		//start moving
		if (game_control.start_dragging_balls) {
			if (game_control.moving_direction == game_control.direction.horizontal) {
				start_dragging_balls(0);
			}
			else {
				if (game_control.moving_direction == game_control.direction.vertical) {
						start_dragging_balls(1);
				}
			}
			game_control.start_dragging_balls = false;
		}

		//move movable balls
		if (game_control.dragging_balls_active) {
			foreach (GameObject ball in game_control.all_balls()) {
				if (game_control.ball_is_movable(ball)) {

					Vector3 start_ball_position = game_control.get_ball(ball).ball_position_when_clicked;
					Vector3 offset = Vector3.Scale(game_control.drag_offset,game_control.direction_to_move_balls);	//drag offset + pravac (x ili y)
					Vector3 new_position = start_ball_position + offset;
					
					ball.transform.position = new_position;
					
					// 同步更新影子副本位置
					update_shadow_clone_position(ball);
				}
			}
		}
		
		// 回退动画（平滑插值到目标位置）
		if (is_snapping) {
			// 从 game_settings 获取回弹速度
			float speed = game_settings.instance != null ? game_settings.instance.snap_speed : 0.15f;
			float threshold = game_settings.instance != null ? game_settings.instance.snap_distance_threshold : 0.01f;
			
			// 真实球回弹
			transform.position = Vector3.Lerp(transform.position, snap_target_position, speed);
			
			// ⭐ 影子球也同步回弹到自己的目标位置
			if (shadow_clone != null) {
				shadow_clone.transform.position = Vector3.Lerp(
					shadow_clone.transform.position, 
					shadow_snap_target, 
					speed
				);
			}
			
			// 当接近目标位置时，停止动画并清理影子
			if (Vector3.Distance(transform.position, snap_target_position) < threshold) {
				transform.position = snap_target_position;
				is_snapping = false;
				
				// 回弹结束后，清理自己的影子副本
				if (shadow_clone != null) {
					Destroy(shadow_clone);
					shadow_clone = null;
				}
				
				// 回弹结束后，重置 is_moving
				is_moving = false;
			}
		}
	}



}
