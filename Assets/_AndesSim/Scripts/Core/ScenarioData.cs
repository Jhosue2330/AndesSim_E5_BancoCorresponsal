using UnityEngine;

namespace AndesSim
{
    /// <summary>
    /// Textos, fechas y cifras de un escenario (contrato común del curso).
    /// Cada escenario puede heredar de esta clase para agregar sus propios datos.
    /// </summary>
    [CreateAssetMenu(menuName = "AndesSim/Scenario Data", fileName = "ScenarioData")]
    public class ScenarioData : ScriptableObject
    {
        [Header("Identificación")]
        public int scenarioNumber = 5;
        public string scenarioName = "Banco Corresponsal Internacional";
        public string scenarioType = "VR";
        public string dateRange = "4 al 8 feb 2016";

        [Header("Historia")]
        [TextArea(3, 8)] public string story;
        [TextArea(1, 3)] public string playerGoal;
        [TextArea(2, 6)] public string[] vulnerabilities;
        [TextArea(2, 6)] public string[] defenses;

        [Header("Resultado")]
        [TextArea(2, 6)] public string resultAttack;
        [TextArea(2, 6)] public string resultDefense;

        [Header("Créditos (autor, licencia y herramientas de IA usadas)")]
        [TextArea(4, 12)] public string credits;
    }
}
