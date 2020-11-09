using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

// public static class ButtonExtension
// {
// 	public static void AddEventListener<T> (this Button button, T param, Action<T> OnClick)
// 	{
// 		button.onClick.AddListener (delegate() {
// 			OnClick (param);
// 		});
// 	}
// }

public class FurItem : MonoBehaviour
{
    [Serializable]
	public struct Game
	{
		public Sprite Icon;
		public GameObject model;
	}

	[SerializeField] Game[] allGames;

	void Start ()
	{
		GameObject buttonTemplate = transform.GetChild(0).gameObject;
		GameObject g;

		int N = allGames.Length;

		for (int i = 0; i < N; i++) {
			g = Instantiate(buttonTemplate, transform);
			g.transform.GetChild(0).GetComponent<Image>().sprite = allGames [i].Icon;
			g.GetComponent<Button>().AddEventListener(i, ItemClicked);
		}

		Destroy(buttonTemplate);
	}

	void ItemClicked (int itemIndex)
	{
		Debug.Log ("------------item " + itemIndex + " clicked---------------");
        GameControl.control.FurPrefab = allGames [itemIndex].model;
		GameControl.control.check = true;
        Debug.Log ($"FurPrefab different null {GameControl.control.FurPrefab != null}");
	}
}
