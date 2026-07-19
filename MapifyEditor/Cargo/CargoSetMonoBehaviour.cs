using System.Collections.Generic;
using UnityEngine;

namespace Mapify.Editor
{
    // I can't with Unity man...
    public class CargoSetMonoBehaviour : MonoBehaviour
    {
        public List<Cargo> cargoTypes;
        public List<string> customCargoTypes;
        public List<Station> stations;

        public CargoSet ToOriginal()
        {
            return new CargoSet {
                cargoTypes = this.cargoTypes,
                customCargoTypes = this.customCargoTypes,
                stations = this.stations
            };
        }
    }
}
