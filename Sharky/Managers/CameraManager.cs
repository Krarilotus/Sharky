﻿namespace Sharky.Managers.Protoss
{
    public class CameraManager : SharkyManager
    {
        SharkyOptions SharkyOptions { get; set; }
        AttackData AttackData { get; set; }

        SC2Action CameraAction { get; set; }

        int FrameLastSet { get; set; }

        public int MaxIdleFrames { get; set; } = 100;

        public CameraManager(DefaultSharkyBot defaultSharkyBot)
        {
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            AttackData = defaultSharkyBot.AttackData;
        }

        public override IEnumerable<SC2Action> OnFrame(ResponseObservation observation)
        {
            if (SharkyOptions.ControlCamera)
            {
                if (CameraAction != null)
                {
                    FrameLastSet = (int)observation.Observation.GameLoop;
                    var actions = new SC2Action[] { CameraAction };
                    CameraAction = null;
                    return actions;
                }

                if (FrameLastSet + MaxIdleFrames < observation.Observation.GameLoop)
                {
                    SetCamera(AttackData.ArmyPoint);
                }
            }

            return null;
        }

        public void SetCamera(Vector2 position)
        {
            SetCamera(position.ToPoint());
        }

        public void SetCamera(Point2D position)
        {
            SetCamera(position.ToPoint());
        }

        public void SetCamera(Point position)
        {
            if (SharkyOptions.ControlCamera)
            {
                CameraAction = new SC2Action
                {
                    ActionRaw = new ActionRaw
                    {
                        CameraMove = new ActionRawCameraMove()
                        {
                            CenterWorldSpace = position
                        }
                    }
                };
            }
        }
    }
}
