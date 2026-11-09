using System;

namespace AndesSim.Scenario05
{
    /// <summary>Qué pasó con la orden en el caso real (2016).</summary>
    public enum OrderFate { Pagada, Bloqueada, NoReenviada }

    /// <summary>Estado de la orden durante la simulación.</summary>
    public enum OrderStatus { EnTransito, Pagada, EnRevision, Bloqueada, Retenida, NoReenviada }

    /// <summary>Orden de pago ficticia que llega al Banco Corresponsal Internacional.</summary>
    [Serializable]
    public class OrderInfo
    {
        public int number;
        public string reference;
        public string beneficiary;
        public string beneficiaryBank;
        public string address;
        public string country;
        public float amountMillions;
        public OrderFate realFate;

        [NonSerialized] public OrderStatus status;
        [NonSerialized] public string flagReason;
        [NonSerialized] public bool blockedByPlayer;

        public string AmountText => $"US$ {amountMillions:0.0} M";
    }
}
