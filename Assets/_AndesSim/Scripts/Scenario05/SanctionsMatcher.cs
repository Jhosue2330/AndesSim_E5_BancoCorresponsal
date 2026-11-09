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
            $"La dirección del beneficiario «{order.address}» contiene «{term}», el nombre de un petrolero " +
            "que figura en una lista de sanciones. El sistema de screening detiene la orden para revisión.\n" +
            "<color=#ffc24a>Ojo:</color> fue una coincidencia de texto con la calle, no una regla contra fraude. " +
            "Las 5 órdenes anteriores no tenían esa palabra y ya se pagaron.";
    }
}
