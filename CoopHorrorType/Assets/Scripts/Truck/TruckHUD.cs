using UnityEngine;

public class TruckHUD : MonoBehaviour
{
    private TruckDepositZone depositZone;

    private void Start()
    {
        depositZone = FindObjectOfType<TruckDepositZone>();
    }

    private void OnGUI()
    {
        if (depositZone == null) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 22;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.yellow;

        int current = depositZone.currentMoney.Value;
        int goal = depositZone.moneyGoal;

        string text = $"💰 ARGENT RÉCOLTÉ : {current}$ / {goal}$";
        
        if (current >= goal)
        {
            text += " - ★ CONTRAT REMPLI ! ★";
            style.normal.textColor = Color.green;
        }

        GUI.Label(new Rect(Screen.width / 2 - 150, 10, 400, 50), text, style);
    }
}