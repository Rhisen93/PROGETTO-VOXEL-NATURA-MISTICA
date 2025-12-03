using UnityEngine;

namespace Elarion.VoxelEngine
{
    /// <summary>
    /// Rappresenta un singolo blocco voxel con tutte le sue proprietà.
    /// Utilizzato come struttura dati leggera per l'engine voxel.
    /// </summary>
    public struct VoxelBlock
    {
        // ═══════════════════════════════════════════════════════════
        // DATI BLOCCO
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>ID univoco del tipo di blocco (0 = aria)</summary>
        public ushort blockID;
        
        /// <summary>Stato del blocco (damage, crescita, etc.)</summary>
        public byte state;
        
        /// <summary>Metadata aggiuntivo (rotazione, variante, etc.)</summary>
        public byte metadata;
        
        // ═══════════════════════════════════════════════════════════
        // COSTRUTTORI
        // ═══════════════════════════════════════════════════════════
        
        public VoxelBlock(ushort id, byte state = 0, byte metadata = 0)
        {
            this.blockID = id;
            this.state = state;
            this.metadata = metadata;
        }
        
        // ═══════════════════════════════════════════════════════════
        // METODI UTILITY
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Ritorna true se il blocco è aria (vuoto)</summary>
        public bool IsAir => blockID == 0;
        
        /// <summary>Ritorna true se il blocco è solido (non trasparente)</summary>
        public bool IsSolid => blockID > 0; // TODO: controllare registro blocchi
        
        /// <summary>Ritorna true se il blocco è trasparente</summary>
        public bool IsTransparent => blockID == 0; // TODO: implementare trasparenza per foglie/acqua
    }
}
