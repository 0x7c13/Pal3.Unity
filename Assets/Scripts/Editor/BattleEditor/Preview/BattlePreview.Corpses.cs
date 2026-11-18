// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using Battle;
    using Core.DataReader.Pol;
    using UnityEngine;

    public sealed partial class BattlePreview
    {
        private readonly Dictionary<string, GameObject> _corpses = new(StringComparer.Ordinal);

        private void ApplyCorpses(BattleSession combat)
        {
            if (combat.Status != BattleSessionStatus.Completed || combat.Result.Winner != BattleSide.Ally && !combat.Result.Escaped)
                return;
            var center = _data.Layout.AllyFormationConfig.CenterGameBoxPosition;
            int ordinal = 0;
            foreach (BattleActorSnapshot actor in combat.Actors)
            {
                if (actor.NativeIndex < 5 || actor.CorpseItem == 0)
                    continue;
                if (!_corpses.TryGetValue(actor.Id, out GameObject model))
                {
                    var item = _data.Database.GameItemInfos[actor.CorpseItem];
                    string path = $"basedata.cpk\\item\\{item.ModelName}\\{item.ModelName}.pol";
                    model = View.CreateObject("Corpse " + actor.Id);
                    try
                    {
                        View.AddPolyModel(model, path, _data.Read(path, new PolFileReader()), item.ModelName + item.ImageId + ".tga");
                    }
                    catch
                    {
                        View.ReleaseCompositeObject(model);
                        throw;
                    }
                    _corpses.Add(actor.Id, model);
                }
                // Both published Scene.Render loops use 20 native units at
                // successive 60-degree angles, two units above the party center.
                double angle = ordinal++ * 60 * (double).01745329238474369f;
                model.transform.position = BattleAnimationCatalog.UnityPosition(new BattleMovementPoint(
                    (float)(center.X + 20 * Math.Sin(angle)), center.Y + 2, (float)(center.Z + 20 * Math.Cos(angle))));
            }
        }
    }
}
