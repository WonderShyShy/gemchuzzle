using UnityEngine;
using System.Collections;

public class ball_sprite_ref : MonoBehaviour {
    public Sprite[] ball_sprite;
    public static ball_sprite_ref object_reference;


    //singleton_objekat
    void Awake()
    {
        object_reference = gameObject.GetComponent<ball_sprite_ref>();
    }
}
