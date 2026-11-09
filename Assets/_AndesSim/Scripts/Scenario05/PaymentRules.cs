using System.Collections.Generic;
using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Defensa «Reglas por destino y monto inusual». Solo actúa si la defensa está aplicada.
    /// </summary>
    public class PaymentRules : MonoBehaviour
    {
        public const string DefenseId = "reglas_destino_monto";

        public bool IsActive => GameManager.I != null && GameManager.I.HasDefense(DefenseId);

        /// <summary>Devuelve el motivo para retener la orden, o null si pasa.</summary>
        public string Evaluate(OrderInfo order, CorresponsalScenarioData d, ISet<string> seenBeneficiaries)
        {
            if (!IsActive || order == null || d == null) return null;
            var reasons = new List<string>();
            bool isNew = !seenBeneficiaries.Contains(order.beneficiary);
            if (isNew && order.amountMillions >= d.unusualAmountMillions)
                reasons.Add($"monto inusual ({order.AmountText}) para un beneficiario nunca usado");
            if (System.Array.IndexOf(d.usualCountries, order.country) < 0)
                reasons.Add($"destino inusual para el BANCO ANDINO ({order.country})");
            return reasons.Count > 0 ? "Regla: " + string.Join(" · ", reasons) : null;
        }
    }
}
