// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Ini;
    using Core.DataReader.Lgt;
    using Core.DataReader.Mv3;
    using Core.DataReader.Pol;
    using Engine.Extensions;
    using Game.Rendering.Native;
    using Game.Rendering.Renderer;
    using ResourceBrowser;
    using UnityEngine;

    // Editor-only choreography. It samples geometry and never dispatches gameplay callbacks.
    public sealed partial class BattlePreview : IDisposable
    {
        private sealed class ActorView
        {
            public BattleParticipant Actor;
            public string SequenceModel;
            public Renderer[] Renderers;
            public BattleSlot DisplaySlot;
            public int Generation;
            public int FormGeneration;
            public bool Visible = true;
            public GameObject Object;
            public Mv3ModelRenderer Renderer;
            public Mv3File Model;
            public string Action;
            public Vector3 Home;
            public Quaternion Rotation;
            public float Duration;
            public bool Loop = true;
        }

        private readonly List<string> _musicDiagnostics = new();
        private readonly List<string> _diagnostics = new();
        private readonly BattleData _data;
        private bool _disposed;
        private readonly BattleScenario _scenario;
        private readonly GameBoxNativeLighting _nativeLighting;
        private BattleCameraProjection? _battleProjection;
        private readonly Dictionary<string, ActorView> _actors = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Mv3File> _models = new(StringComparer.OrdinalIgnoreCase);
        private string _attacker;
        private string _environmentScene;
        private Vector3 _approach;
        private float _travel;
        private float _attackDuration;
        private string _attackAction, _moveAction;

        public ResourceModelPreview View
        {
            get;
        }

        public BattleEffectDirector Effects
        {
            get;
        }

        public BattleAudioPreview Audio
        {
            get;
        }

        public BattleTrailPreview Trail
        {
            get;
        }

        public BattleCameraDirector Camera
        {
            get; private set;
        }

        public float Time
        {
            get; private set;
        }

        public float Duration { get; private set; } = 5;

        public IReadOnlyList<string> Diagnostics => _diagnostics.Concat(Audio.Diagnostics).Concat(Effects.MovieDiagnostics)
            .Concat(Effects.MissingBattleTextures.Select(texture => "缺少战斗贴图：" + texture)).Distinct().ToArray();

        public BattlePreview(BattleData data, BattleSetup setup)
        {
            _data = data;
            _scenario = setup.CreateInstance();
            try
            {
                _nativeLighting = _scenario.ToonRendering ? null : new GameBoxNativeLighting(BattleData.Variant);
                View = new ResourceModelPreview(data.Resources, _scenario.ToonRendering, _nativeLighting) { LimitToDisplayRefreshRate = true };
                Trail = new BattleTrailPreview();
                View.FrameEffect = Trail;
                Audio = new BattleAudioPreview(data.Resources, View.Root.scene) { PlaybackEnabled = false };
                Effects = new BattleEffectDirector(data, View, Audio);
                Effects.SpiritProjector = (_, world) => _battleProjection?.Project(world)
                    ?? throw new InvalidOperationException("Spirit capture requires a published battle camera.");
                Effects.SoundRequested += Audio.Play;
                Effects.MovementSoundRequested += Audio.PlayMovement;
                Effects.MovementSoundStopped += Audio.StopMovement;
                Effects.BodySoundsStopped += Audio.StopBodySounds;
                View.Rendering += Effects.Render;
                View.Rendering += RenderWeaponBlades;
                View.Rendering += RenderWeaponSparks;
                View.PostComposite += RenderHitFeedback;
                Audio.SetMusic(BattleMusicResolver.Resolve(data, _scenario, _musicDiagnostics));
                LoadEnvironment(_scenario.Scene);
                foreach (BattleParticipant actor in _scenario.Participants)
                {
                    var view = new ActorView { Actor = actor, SequenceModel = data.Model(actor), DisplaySlot = actor.Slot, Object = View.CreateObject(actor.InstanceId), Home = data.Position(actor, _scenario.SceneElement) };
                    bool forward = actor.Side == BattleSide.Ally ? _scenario.Encounter != BattleEncounter.EnemyAmbush : _scenario.Encounter == BattleEncounter.AllyAmbush;
                    view.Rotation = Quaternion.Euler(0, forward ? -45 : 135, 0);
                    _actors.Add(actor.InstanceId, view);
                    SetAction(view, data.IdleAction(actor));
                }
                InitializeShadows();
                BattleCamera camera = _scenario.Camera;
                if (camera.Override)
                    View.SetCamera(ToVector(camera.Position), ToVector(camera.Target), camera.FieldOfView);
                else if (data.Cameras.DefaultCamConfigs.Length > 0)
                {
                    CombatCameraConfig original = data.Cameras.DefaultCamConfigs[0];
                    ApplyCameraPose(new BattleCameraPose(new BattleMovementPoint(original.GameBoxPositionX,
                        original.GameBoxPositionY, original.GameBoxPositionZ), original.Yaw, original.Pitch, original.Roll), 50, false);
                }
                Sample(0);
                CollectDependencies();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void LoadEnvironment(string scene)
        {
            BattleSceneAppearance appearance = _scenario.ToonRendering ? BattleToonLightingCatalog.ResolveScene(BattleData.Variant, scene) : null;
            string path = FileConstants.CombatSceneFolderVirtualPath + scene + "\\" + scene;
            string geometry = path + ".pol";
            string lights = path + ".lgt";
            PolFile model = _data.Read(geometry, new PolFileReader());
            LgtFile lightData = _nativeLighting != null && _data.Resources.Find(lights) != null ? _data.Read(lights, new LgtFileReader()) : null;
            // Both published loaders create a CVD object but never attach
            // it during combat rendering. Render the verified scene geometry.
            Func<int, int, Pal3.Game.Rendering.Renderer.PolSurfaceAppearance?> surfaces = appearance?.ResolveSurfaces(model);
            View.ReplaceEnvironment(_data.Resources.Find(geometry), model, surfaces);
            // Bindings select lights during Apply, after this transaction. A
            // failed geometry replacement must keep the previous scene's lights.
            _nativeLighting?.SetLights(lightData);
            if (appearance != null)
                View.ConfigureToonLighting(appearance.Lighting);
            _environmentScene = scene;
        }

        internal string ActorModel(string id) => _actors.TryGetValue(id, out ActorView view) ? view.SequenceModel : null;

        private ActorView BattleActorView(BattleSession combat, BattleActorSnapshot state)
        {
            int generation = combat.MonsterGeneration(state.Id);
            BattlePal3AFormSnapshot form = combat.Pal3AForm(state.Id);
            _actors.TryGetValue(state.Id, out ActorView previous);
            if (previous != null && previous.Renderer != null && previous.Renderers != null &&
                previous.Generation == generation && previous.FormGeneration == form.Generation)
                return previous;
            BattleParticipant participant = combat.CaptureParticipant(state.Id);
            string sequenceModel = _data.Model(participant);
            if (!string.IsNullOrEmpty(form.Model))
                participant.Model = form.Model;
            var candidate = new ActorView
            {
                Actor = participant,
                SequenceModel = sequenceModel,
                DisplaySlot = state.Slot,
                Generation = generation,
                FormGeneration = form.Generation,
                Object = View.CreateObject(state.Id),
                Home = BattleAnimationCatalog.UnityPosition(state.Home),
                Rotation = Quaternion.Euler(0, -state.FacingDegrees, 0)
            };
            try
            {
                SetAction(candidate, _data.IdleAction(participant));
                candidate.Object.transform.SetPositionAndRotation(BattleAnimationCatalog.UnityPosition(state.Position), candidate.Rotation);
            }
            catch
            {
                View.ReleaseCompositeObject(candidate.Object);
                throw;
            }
            _actors[state.Id] = candidate;
            RefreshShadowsForActorReplacement(previous, candidate);
            if (previous != null)
                View.ReleaseCompositeObject(previous.Object);
            return candidate;
        }

        public void BindBattle(BattleSession combat) => BindBattle(combat, null);

        public void BindBattle(BattleSession combat, BattleAnimationCatalog catalog)
        {
            if (Camera != null)
                throw new InvalidOperationException("This preview already owns a battle camera.");
            View.GetCamera(out Vector3 position, out Vector3 target, out float fieldOfView);
            BattleMovementPoint Native(Vector3 value) => new(-value.x * UnityPrimitivesConvertor.GameBoxUnitToUnityUnit,
                value.y * UnityPrimitivesConvertor.GameBoxUnitToUnityUnit, value.z * UnityPrimitivesConvertor.GameBoxUnitToUnityUnit);
            BattleMovementPoint direction = Native((target - position).normalized);
            var initial = new BattleCameraPose(Native(position), MathF.Atan2(direction.X, -direction.Z),
                MathF.Atan2(direction.Y, MathF.Sqrt(direction.X * direction.X + direction.Z * direction.Z)), 0);
            if (!_scenario.Camera.Override && _data.Cameras.DefaultCamConfigs.Length > 0)
            {
                CombatCameraConfig original = _data.Cameras.DefaultCamConfigs[0];
                initial = new BattleCameraPose(new BattleMovementPoint(original.GameBoxPositionX, original.GameBoxPositionY,
                    original.GameBoxPositionZ), original.Yaw, original.Pitch, original.Roll);
            }
            BattleMovementPoint TracePosition(int index)
            {
                if (index == -1)
                {
                    var center = _data.Layout.AllyFormationConfig.CenterGameBoxPosition;
                    return new BattleMovementPoint(center.X, center.Y, center.Z);
                }
                string actor = combat.EffectActorId(index);
                return actor == null ? default : combat.EffectActor(actor).Position;
            }
            // Gameplay projection always uses its fixed reference canvas. The
            // editor viewport must not change capture physics or list startup.
            float horizontalFieldOfView = _scenario.Camera.Override
                ? UnityEngine.Camera.VerticalToHorizontalFieldOfView(_scenario.Camera.FieldOfView, 4f / 3) : 50;
            Camera = new BattleCameraDirector(_data, initial, TracePosition, ApplyCameraPose, horizontalFieldOfView);
            Effects.Bind(combat, Camera);
            Effects.StageAdvanced += Camera.Advance;
            Effects.StageAdvanced += Trail.Advance;
            if (catalog != null)
                Effects.StageAdvanced += (_, _) => ApplyBattle(combat, catalog);
            if (!_scenario.Camera.Override)
                Camera.StartList(_scenario.Encounter == BattleEncounter.EnemyAmbush && _scenario.MotionBlur ? 20 : 0);
        }

        private void ApplyCameraPose(BattleCameraPose pose, float fieldOfView, bool trail)
        {
            _battleProjection = new BattleCameraProjection(pose, fieldOfView);
            Trail.Enabled = trail && _scenario.MotionBlur;
            Vector3 Direction(BattleMovementPoint point) => new(-point.X, point.Y, point.Z);
            // An authored camera stores a Unity vertical angle. Keep that
            // framing through binding and idle/quake updates; a successfully
            // started native list takes over with GameBox horizontal angles.
            if (_scenario.Camera.Override && Camera?.HasStartedList != true)
                View.SetCameraOrientation(BattleAnimationCatalog.UnityPosition(pose.Position), Direction(pose.Forward), Direction(pose.Up), _scenario.Camera.FieldOfView);
            else
                View.SetHorizontalCameraOrientation(BattleAnimationCatalog.UnityPosition(pose.Position), Direction(pose.Forward), Direction(pose.Up), fieldOfView);
        }

        private void RenderWeaponBlades(Camera camera)
        {
            bool published = BattleData.Variant == "PAL3";
            bool newFrame = _lastBladeRenderTime != _bladeFrameTime;
            float delta = !_lastBladeRenderTime.HasValue || _bladeFrameTime < _lastBladeRenderTime.Value ? (float)BattleSession.StepSeconds :
                (float)(_bladeFrameTime - _lastBladeRenderTime.Value);
            foreach (ActorView actor in _actors.Values)
                if (published)
                    actor.Renderer.RenderPublishedWeaponBlades(camera, delta, newFrame, actor.Visible);
                else if (actor.Visible)
                    actor.Renderer.RenderWeaponBlades(camera);
            RenderThrownWeaponBlades(camera, delta, newFrame);
            _lastBladeRenderTime = _bladeFrameTime;
        }

        private Mv3File ReadActorModel(string path)
        {
            if (!_models.TryGetValue(path, out Mv3File model))
                _models[path] = model = _data.Read(path, new Mv3FileReader(includeNativeNormals: _nativeLighting != null));
            return model;
        }

        private void SetAction(ActorView actor, string action)
        {
            string path = _data.ActionPath(actor.Actor, action);
            Mv3File model = ReadActorModel(path);
            string weaponPath = null;
            string weaponTexture = null;
            PolFile weapon = null;
            if (model.TagNodes.Length > 0 && actor.Actor.Equipment is { Length: > 0 } && actor.Actor.Equipment[0] != 0)
            {
                var item = _data.Database.GameItemInfos[actor.Actor.Equipment[0]];
                weaponPath = FileConstants.GetWeaponModelFileVirtualPath(item.ModelName);
                weaponTexture = item.ModelName + item.ImageId + ".tga";
                weapon = _data.Read(weaponPath, new PolFileReader());
            }
            actor.Renderer = View.SetVertexModel(actor.Object, path, model, weaponPath, weapon, preserveWeaponPresentation: true, weaponTexture: weaponTexture);
            actor.Renderers = actor.Object.GetComponentsInChildren<Renderer>(true);
            actor.Renderer.UseRenderBladeClock = BattleData.Variant == "PAL3";
            actor.Model = model;
            actor.Action = action;
            actor.Duration = (float)model.TotalGameBoxTicks / UnityPrimitivesConvertor.GameBoxTicksPerSecond;
        }

        public void PlayAction(string instanceId, string action)
        {
            ResetAuthoringActors();
            ActorView actor = _actors[instanceId];
            SetAction(actor, action);
            actor.Loop = false;
            Duration = Mathf.Max(0.02f, actor.Duration);
            Sample(0);
            CollectDependencies();
        }

        public void PlayAttack(string attacker, string target)
        {
            if (!_actors.TryGetValue(attacker, out ActorView source) || !_actors.TryGetValue(target, out ActorView destination) || source.Actor.Side == destination.Actor.Side)
                throw new InvalidOperationException("Select an attacker and an opposing target.");
            Vector3 direction = (source.Home - destination.Home).normalized;
            if (direction.sqrMagnitude < 0.01f)
                throw new InvalidOperationException("Attack preview needs distinct source and target positions.");
            string moveAction = ActorConstants.ActionToNameMap[_data.Database.CombatActorInfos[source.Actor.ActorId].Type == CombatActorType.MainActor ? ActorActionType.AttackMove : ActorActionType.NpcRun];
            string attackAction = _data.AttackAction(source.Actor);
            ReadActorModel(_data.ActionPath(source.Actor, moveAction));
            ReadActorModel(_data.ActionPath(source.Actor, attackAction));

            ResetAuthoringActors();
            SetAction(source, attackAction);
            source.Loop = false;
            _attackAction = source.Action;
            _attackDuration = source.Duration;
            _moveAction = moveAction;
            Vector3 sourceSize = source.Renderer.GetMeshBounds().size;
            Vector3 targetSize = destination.Renderer.GetMeshBounds().size;
            float separation = Mathf.Max(0.1f, (Mathf.Max(sourceSize.x, sourceSize.z) + Mathf.Max(targetSize.x, targetSize.z)) / 2);
            _approach = destination.Home + direction * separation;
            _travel = Vector3.Distance(source.Home, _approach) / 12;
            _attacker = attacker;
            Duration = _travel * 2 + _attackDuration;
            Sample(0);
            CollectDependencies();
        }

        public void Reset()
        {
            ResetAuthoringActors();
            Sample(0);
        }

        private void ResetAuthoringActors()
        {
            _attacker = null;
            ResetAuthoringWeaponBlades();
            Duration = 5;
            foreach (ActorView actor in _actors.Values)
            {
                string idle = _data.IdleAction(actor.Actor);
                if (actor.Action != idle)
                    SetAction(actor, idle);
                actor.Loop = true;
            }
        }

        private void ResetAuthoringWeaponBlades()
        {
            _lastBladeRenderTime = null;
            foreach (ActorView actor in _actors.Values)
                actor.Renderer.ResetWeaponPresentation();
        }

        public void Sample(float time)
        {
            if (!float.IsFinite(time))
                throw new ArgumentOutOfRangeException(nameof(time));
            float next = Mathf.Clamp(time, 0, Duration);
            if (next < Time)
                ResetAuthoringWeaponBlades();
            Time = next;
            Audio.SetTime(Time);
            SetBladeFrameTime(Time);
            foreach (ActorView actor in _actors.Values)
            {
                actor.Object.transform.SetPositionAndRotation(actor.Home, actor.Rotation);
                float local = Time;
                if (actor.Actor.InstanceId == _attacker)
                {
                    bool returning = Time > _travel + _attackDuration;
                    bool finished = Time >= Duration;
                    string action = finished ? _data.IdleAction(actor.Actor) : Time < _travel || returning ? _moveAction : _attackAction;
                    if (actor.Action != action)
                        SetAction(actor, action);
                    if (Time < _travel)
                        actor.Object.transform.position = Vector3.Lerp(actor.Home, _approach, _travel > 0 ? Time / _travel : 1);
                    else if (returning)
                        actor.Object.transform.position = Vector3.Lerp(_approach, actor.Home, _travel > 0 ? (Time - _travel - _attackDuration) / _travel : 1);
                    else
                        actor.Object.transform.position = _approach;
                    if (!finished)
                        actor.Object.transform.LookAt(returning ? actor.Home : _approach + (_approach - actor.Home).normalized);
                    local = Time < _travel ? Time : returning ? Time - _travel - _attackDuration : Time - _travel;
                    local = Time < _travel || returning ? actor.Duration > 0 ? local % actor.Duration : 0 : Mathf.Clamp(local, 0, _attackDuration);
                    if (finished)
                    {
                        actor.Object.transform.rotation = actor.Rotation;
                        local = 0;
                    }
                }
                actor.Renderer.SampleAnimation(actor.Loop && actor.Duration > 0 ? local % actor.Duration : Mathf.Clamp(local, 0, actor.Duration));
                actor.Renderer.SampleWeaponBlades(Time, 1, .12f);
            }
            SampleShadows();
        }

        public void ApplyBattle(BattleSession combat, BattleAnimationCatalog catalog)
        {
            Audio.SetTime(combat.Tick * BattleSession.StepSeconds);
            Time = (float)(combat.Tick * BattleSession.StepSeconds);
            SetBladeFrameTime(combat.Tick * BattleSession.StepSeconds);
            Trail.Suspended = combat.IsModalPresentation;
            if (_environmentScene != combat.ActiveScene)
                LoadEnvironment(combat.ActiveScene);
            foreach (BattleActorSnapshot state in combat.Actors)
            {
                ActorView actor = BattleActorView(combat, state);
                actor.DisplaySlot = state.Slot;
                BattleEffectActorSnapshot effectActor = combat.EffectActor(state.Id);
                // The body binding owns the actual animation, including poses
                // such as GuardHurt which have no generic catalog fallback.
                string clip = state.NativeAnimationClip ?? effectActor?.Clip ?? catalog.Clip(state, combat.Definitions);
                if (state.Side == BattleSide.Enemy && !state.ModelAliveFlag)
                    clip = null;
                actor.Visible = clip != null && !state.Escaped && combat.CoAttackActorVisible(state.Id);
                if (clip == null)
                {
                    foreach (Renderer renderer in actor.Renderers)
                        renderer.enabled = false;
#if PAL3
                    actor.Renderer.SampleWeaponBlades(state.NativePresentationTick * BattleSession.StepSeconds,
                        state.NativePlaybackRate, state.WeaponTrailSeconds);
#endif
                    continue; // Original monsters disappear; retained blade records still age.
                }
                if (actor.Action != clip)
                    SetAction(actor, clip);
                foreach (Renderer renderer in actor.Renderers)
                    renderer.enabled = actor.Visible;
                actor.Object.transform.SetPositionAndRotation(BattleAnimationCatalog.UnityPosition(state.Position),
                    Quaternion.Euler(0, -state.FacingDegrees, 0));
                float opacity = state.Movement?.Blend ?? (state.States.Any(status => status.State == BattleState.Invisible) ? 0.44f : 1f);
                if (effectActor?.HasBlend == true)
                    opacity = effectActor.Blend;
                actor.Renderer.SetOpacity(opacity);
                BattleWeaponVisibility weapons = combat.WeaponVisibility(state.Id);
                actor.Renderer.SetWeaponVisibility(actor.Visible && weapons.Left, actor.Visible && weapons.Right);
                float local = (float)(state.PoseTick * BattleSession.StepSeconds * catalog.PlaybackRate(state));
                if (effectActor?.Clip != null)
                    local = (float)(effectActor.AnimationTick * BattleSession.StepSeconds * catalog.PlaybackRate(state.Id, BattlePose.UsingSkill));
                bool loop = state.Pose is BattlePose.Idle or BattlePose.Guard or BattlePose.Moving or BattlePose.Returning or BattlePose.Casting or BattlePose.Fleeing || state.Pose == BattlePose.Repositioning && state.Movement?.Plan.Kind is BattleMovementKind.Run or BattleMovementKind.Teleport;
                if (effectActor?.Clip != null)
                    loop = false;
                if (state.NativeAnimationClip != null)
                {
                    local = (float)state.NativeAnimationSeconds;
                    loop = false;
                }
                if (state.Pose == BattlePose.Dead && (actor.Actor.Vitals[0] == 0 || combat.Status != BattleSessionStatus.Running))
                    local = actor.Duration;
                actor.Renderer.SampleAnimation(loop && actor.Duration > 0 ? local % actor.Duration : Mathf.Clamp(local, 0, actor.Duration));
                actor.Renderer.SampleWeaponBlades(state.NativePresentationTick * BattleSession.StepSeconds,
                    state.NativePlaybackRate, state.WeaponTrailSeconds);
            }
            ApplyShadows(combat);
            ApplyThrownWeapons(combat);
            ApplyCorpses(combat);
            ApplyNumbers(combat);
            ApplyMessages(combat);
            ApplyHitFeedback(combat);
            ApplySwordSparks(combat);
            ApplyLineups(combat);
            CollectDependencies();
            _diagnostics.AddRange(catalog.Diagnostics.Distinct());
        }

        public string[] AnimationMarkers(string instanceId)
        {
            if (!_actors.TryGetValue(instanceId, out ActorView actor))
                return Array.Empty<string>();
            return actor.Model.AnimationEvents.Select(marker => $"{marker.GameBoxTick / (float)UnityPrimitivesConvertor.GameBoxTicksPerSecond:F3}s · {marker.Name}").ToArray();
        }

        public string InstanceAt(Vector2 point, Rect rect)
        {
            return _actors.Values.Where(actor => actor.Visible && View.Project(actor.Object.transform.position + Vector3.up, rect, out Vector2 projected) && Vector2.Distance(projected, point) < 24)
                .OrderBy(actor =>
                {
                    View.Project(actor.Object.transform.position + Vector3.up, rect, out Vector2 projected);
                    return Vector2.Distance(projected, point);
                })
                .Select(actor => actor.Actor.InstanceId).FirstOrDefault();
        }

        public void DrawLabels(Rect rect, string selected)
        {
            if (rect.width <= 0 || rect.height <= 0)
                return;
            // Scene labels need a consistent contrast over the rendered world,
            // independent of the Editor skin's box textures.
            var style = new GUIStyle(GUIStyle.none)
            {
                font = GUI.skin.label.font,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = Color.white },
                hover = { textColor = Color.white }
            };
            foreach (ActorView actor in _actors.Values)
                if (actor.Visible && View.Project(actor.Object.transform.position + Vector3.up * 2, rect, out Vector2 point))
                {
                    string text = $"{(actor.Actor.InstanceId == selected ? "▶ " : "")}{_data.Database.CombatActorInfos[actor.Actor.ActorId].Name} · {actor.DisplaySlot}";
                    var content = new GUIContent(text, text);
                    Vector2 required = style.CalcSize(content);
                    float width = Math.Min(rect.width, Math.Max(140, required.x + 12));
                    float height = Math.Min(rect.height, Math.Max(22, required.y));
                    var label = new Rect(Mathf.Clamp(point.x - width / 2, rect.xMin, rect.xMax - width),
                        Mathf.Clamp(point.y - height / 2, rect.yMin, rect.yMax - height), width, height);
                    if (Event.current.type == EventType.Repaint)
                        UnityEditor.EditorGUI.DrawRect(label, new Color(.06f, .07f, .09f, .8f));
                    GUI.Label(label, content, style);
                }
        }

        private void CollectDependencies()
        {
            _diagnostics.Clear();
            _diagnostics.AddRange(_musicDiagnostics);
            _diagnostics.AddRange(View.Dependencies.Where(pair => !pair.Value).Select(pair => "Missing texture dependency: " + pair.Key));
        }

        private static Vector3 ToVector(BattlePoint point) => new(point.X, point.Y, point.Z);

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Effects?.Dispose();
            Audio?.Dispose();
            Trail?.Dispose();
            DisposeNumbers();
            DisposeSwordSparks();
            DisposeHitFeedback();
            DisposeLineups();
            DisposeThrownWeapons();
            DisposeShadows();
            View?.Dispose();
            _nativeLighting?.Dispose();
        }
    }
}
