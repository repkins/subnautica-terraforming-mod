using System;
using System.Collections.Generic;
using System.Text;

namespace TerraformingShared.Tools.Building
{
    static class EntityCellExtensions
    {
        public static readonly string PassThroughColliderName = "TerraformingHelper";

        public static void Deconstruct(this EntityCell entityCell, out int level, out Int3 cellId, out Int3 batchId)
        {
            level = entityCell.level;
            cellId = entityCell.CellId;
            batchId = entityCell.BatchId;
        }

        public static (int level, Int3 cellId, Int3 batchId) GetTuple(this EntityCell entityCell)
        {
            return (entityCell.level, entityCell.CellId, entityCell.BatchId);
        }
    }
}
