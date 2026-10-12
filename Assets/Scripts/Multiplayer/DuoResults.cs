using TMPro;
using UnityEngine;

//Duo mode card on the results screen (built by ResultsScreen): your score, your partner's, and the team total.
//The partner's final score may arrive a moment after this screen opens; it fills in when it does.
public class DuoResults : MonoBehaviour
{
    private TextMeshProUGUI partnerText, teamText;
    private bool showedFinal;

    public void Build(RectTransform r)
    {
        DuoUI.Panel(r, 0, 0, 428, 172, Theme.Surface);
        TextMeshProUGUI cap = DuoUI.Label(r, "DUO  ·  YOU: " + NetSession.RoleName(NetSession.MyRole).ToUpperInvariant(), 0, 58, 400, 30, 18, Theme.Accent, true);
        cap.characterSpacing = 4;
        DuoUI.Label(r, "YOU  " + DuoUI.Thousands(ScoreKeeper.score), 0, 22, 400, 34, 24, Theme.Text);
        partnerText = DuoUI.Label(r, "", 0, -12, 400, 34, 24, Theme.TextMuted);
        teamText = DuoUI.Label(r, "", 0, -54, 400, 46, 36, Theme.Text, true);
        Refresh();
    }

    void Update()
    {
        if (!showedFinal && (NetSession.PartnerFinished || !NetSession.Connected)) Refresh();
    }

    void Refresh()
    {
        if (partnerText == null) return;
        string role = NetSession.RoleName(NetSession.PartnerRole).ToUpperInvariant();
        if (NetSession.PartnerFinished)
        {
            showedFinal = true;
            partnerText.text = role + "  " + DuoUI.Thousands(NetSession.PartnerFinalScore) + "   (x" + NetSession.PartnerFinalCombo + ")";
            teamText.text = "TEAM  " + DuoUI.Thousands(ScoreKeeper.score + NetSession.PartnerFinalScore);
        }
        else if (!NetSession.Connected)
        {
            showedFinal = true;
            partnerText.text = role + "  " + DuoUI.Thousands(NetSession.PartnerScore) + "  (disconnected)";
            teamText.text = "TEAM  " + DuoUI.Thousands(ScoreKeeper.score + NetSession.PartnerScore);
        }
        else
        {
            partnerText.text = role + "  still playing...";
            teamText.text = "TEAM  " + DuoUI.Thousands(ScoreKeeper.score + NetSession.PartnerScore);
        }
    }
}
