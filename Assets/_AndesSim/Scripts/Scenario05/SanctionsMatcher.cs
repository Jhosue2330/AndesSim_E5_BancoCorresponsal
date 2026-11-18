using UnityEngine;

namespace AndesSim.Scenario05
{
    /// <summary>
    /// Compara los textos de la orden (beneficiario y dirección) con la lista de sanciones.
    /// Es una coincidencia de TEXTO: «Calle Jupiter» coincide con el petrolero «JUPITER».
    /// No detectó el fraude como tal, pero frenó los pagos.
    /// </summary>
    public class SanctionsMatcher : MonoBehaviour
    {
        /// <summary>Devuelve el término que coincide o null.</summary>
        public string Match(OrderInfo order, string[] sanctionsList)
        {
            if (order == null || sanctionsList == null) return null;
            string haystack = $"{order.beneficiary} {order.address}".ToUpperInvariant();
            foreach (var term in sanctionsList)
            {
                if (string.IsNullOrWhiteSpace(term)) continue;
                if (haystack.Contains(term.Trim().ToUpperInvariant())) return term.Trim().ToUpperInvariant();
            }
            return null;
        }

        public static string Explain(OrderInfo order, string term) =>
            $"«{term}» en la dirección = petrolero sancionado.\nCoincidencia de texto, no regla antifraude.";
    }
}
