/*
 * Author: Viacheslav Soroka
 * 
 * This file is part of IGE <https://github.com/destrofer/IGE>.
 * 
 * IGE is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * IGE is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with IGE.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Collections.Generic;

using IGE;
using IGE.Graphics;

namespace IGE.Universe {
	public interface ITileDef {
		/// <summary>
		/// ID is assigned by tile database and should be saved in the tile definition object
		/// </summary>
		int ID { get; set; }
		
		/// <summary>
		/// Keyword is a unique tile name that will be used to identify tile in saves
		/// </summary>
		string Keyword { get; }
		
		SimpleImage Image { get; }
		
		ITileInstance CreateTileInstance(BaseGameMap map, int x, int y, int z);
	}

	/*
 	public interface ISectorizedMap : Map {
		Dictionary<ulong, IMapSector> Sectors { get; }
		List<IMapSector> ActiveSectors { get; }
		List<IMapSector> VisibleSectors { get; }

		ulong GetSectorId(int x, int y, int z);
		ulong GetSectorId(Point3 coords);
		Point3 GetSectorCoords(ulong sectorId);
		// BBox GetSectorBoundingBox(ulong sectorId);
	}

	public interface IMapSector : ISerializable {
		ulong ID { get; }
		Dictionary<string, ISerializable> Meta { get; }
		
		/// <summary>
		/// May be used to track active sectors so that they would "think" and
		/// be kept in cache for that duration.
		/// </summary>
		double DeactivationTime { get; set; }

		/// <summary>
		/// Tells if sector is visible by player in his current view.
		/// </summary>
		/// <param name="map">Map that contains this sector</param>
		/// <returns>true if sector is visible</returns>
		bool IsVisible(ISectorizedMap map);
		
		/// <summary>
		/// Activates thinking process of tiles and objects in this sector.
		/// </summary>
		/// <param name="map"></param>
		void Think(ISectorizedMap map);
		void Render(ISectorizedMap map);
	}

	public interface IMapGenerator<TSector> : ISerializable where TSector : IMapSector {
		TSector Generate(TSector sector);
	}

	public interface ITileMap : ISectorizedMap {
		Dictionary<ulong, ITileInstance> TileInstances { get; }
	}
	*/
}
