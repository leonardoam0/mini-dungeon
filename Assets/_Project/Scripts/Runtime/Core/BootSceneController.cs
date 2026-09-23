using System.Collections;
using UnityEngine;

namespace Ruinas
{
    /// <summary>Cena de inicialização: mostra o título e encaminha ao menu ou ao modo pedido na linha de comando.</summary>
    public class BootSceneController : MonoBehaviour
    {
        IEnumerator Start()
        {
            var canvas = UIFactory.CreateCanvas("Boot", 10);
            var bg = UIFactory.Image("Fundo", canvas.transform, UIFactory.Skin != null ? UIFactory.Skin.white : null, new Color(0.02f, 0.06f, 0.055f), false);
            bg.rectTransform.Stretch();
            var title = UIFactory.Text("Titulo", canvas.transform, "RUÍNAS DO OBELISCO", 7f, UIFactory.TextGold, TextAnchor.MiddleCenter);
            title.rectTransform.Stretch(0, 0, 0, 60);
            title.Outline = true;
            var sub = UIFactory.Text("Sub", canvas.transform, "um diorama de masmorra em blocos", 3f, UIFactory.TextDim, TextAnchor.MiddleCenter);
            sub.rectTransform.Stretch(0, 120, 0, 0);

            yield return new WaitForSecondsRealtime(0.7f);
            var a = Services.Args;
            if (a != null && (a.StartReference || a.Capture))
                Services.Flow.Load(new LoadRequest { scene = SceneFlow.ReferenceScene, referenceReplay = true });
            else if (a != null && (a.AutoPlay || a.StartMission || a.TestSaveLoad))
                Services.Flow.Load(new LoadRequest { scene = SceneFlow.MissionScene, mode = a.ContinueMission ? MissionStartMode.Continue : MissionStartMode.NewGame });
            else if (a != null && a.ContinueMission)
                Services.Flow.Load(new LoadRequest { scene = SceneFlow.MissionScene, mode = MissionStartMode.Continue });
            else
                Services.Flow.Load(new LoadRequest { scene = SceneFlow.MenuScene });
        }
    }
}
