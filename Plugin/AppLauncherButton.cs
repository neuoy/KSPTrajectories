namespace Trajectories
{
        private class BlizzyToolbarButtonVisibility: IVisibility
        {
            // permit global access
            private static BlizzyToolbarButtonVisibility instance = null;

            private static IVisibility flight_visibility;

            public static BlizzyToolbarButtonVisibility Instance
            {
                get
                {
                    return instance;
                }
            }

            //  constructor
            public BlizzyToolbarButtonVisibility()
            {
                // enable global access
                instance = this;

                flight_visibility = new GameScenesVisibility(GameScenes.FLIGHT);
            }

            public bool Visible
            {
                get
                {
                    return flight_visibility.Visible;
                }
            }
        }
}
