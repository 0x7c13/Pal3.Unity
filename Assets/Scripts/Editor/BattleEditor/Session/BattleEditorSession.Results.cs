// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using Battle;

    public sealed partial class BattleEditorSession
    {
        private double _resultAccumulator;

        public BattleResultPresentation ResultPresentation
        {
            get; private set;
        }

        private void ResetResults()
        {
            ResultPresentation = null;
            _resultAccumulator = 0;
        }

        private void EnsureResults()
        {
            if (ResultPresentation != null || Battle?.Status != BattleSessionStatus.Completed)
                return;
            ResultPresentation = new BattleResultPresentation(Battle.Definitions?.Variant ?? _setup.CreateInstance().Variant, Battle.Result, Battle.Events, Preview);
            _resultAccumulator = 0;
            Preview.BeginResultPresentation(Battle, _battleCatalog, ResultPresentation);
        }

        public void ReplayResults()
        {
            if (Battle?.Status == BattleSessionStatus.Completed)
                BeginReplay();
        }

        public bool SkipResultStep()
        {
            if (ResultPresentation?.SkipCurrent() != true)
                return false;
            Preview.ApplyResultPresentation(ResultPresentation, false);
            FinishResults();
            return true;
        }

        private void StepResults()
        {
            if (ResultPresentation == null || ResultPresentation.Completed)
                return;
            Preview.PrepareResultBodyPass();
            ResultPresentation.Step();
            Preview.ApplyResultPresentation(ResultPresentation, true);
            FinishResults();
        }

        private void FinishResults()
        {
            if (ResultPresentation?.Completed != true)
                return;
            Preview.Audio.Stop();
            _resultAccumulator = 0;
            IsPlaying = false;
        }

        private void AdvanceResults(double seconds)
        {
            if (!double.IsFinite(seconds) || seconds < 0 || seconds > 60)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            _resultAccumulator += seconds;
            while (_resultAccumulator + 1e-12 >= BattleSession.StepSeconds && !ResultPresentation.Completed)
            {
                _resultAccumulator -= BattleSession.StepSeconds;
                StepResults();
            }
        }
    }
}
