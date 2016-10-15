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
using System.Collections.Generic;

namespace IGE.Universe {
	/// <summary>
	/// Used to register and search tile definitions.
	/// </summary>
	public class TileDB {
		protected List<ITileDef> m_UncompiledTileDefs = new List<ITileDef>();
		protected ITileDef[] m_TileDefs = null;
		protected Dictionary<string, ITileDef> m_TileIndex = new Dictionary<string, ITileDef>();
		protected int m_TileCount = 0;
		
		public int TileCount { get { return m_TileCount; } }
		
		/// <summary>
		/// Returns ITileDef object for a given id. Will always return null for 0 and will throw exceptions
		/// in case when the database was not compiled (TileDB.Compile) or id is outside of database bounds.
		/// </summary>
		public virtual ITileDef this[int id] {
			get { return m_TileDefs[id]; }
		}
		
		/// <summary>
		/// Returns ITileDef object for a given id. Will always return null for 0 and will throw exceptions
		/// in case when the database was not compiled (TileDB.Compile) or id is outside of database bounds.
		/// </summary>
		public virtual ITileDef this[short id] {
			get { return m_TileDefs[(int)id]; }
		}
		
		/// <summary>
		/// Returns ITileDef object for a given id. Will always return null for 0 and will throw exceptions
		/// in case when the database was not compiled (TileDB.Compile) or id is outside of database bounds.
		/// </summary>
		public virtual ITileDef this[ushort id] {
			get { return m_TileDefs[(int)id]; }
		}
		
		/// <summary>
		/// Returns ITileDef object for a given id. Will always return null for 0 and will throw exceptions
		/// in case when the database was not compiled (TileDB.Compile) or id is outside of database bounds.
		/// </summary>
		public virtual ITileDef this[byte id] {
			get { return m_TileDefs[(int)id]; }
		}
		
		/// <summary>
		/// Returns ITileDef object for a given keyword. May be called without compiling the database.
		/// </summary>
		public virtual ITileDef this[string keyword] {
			get { return m_TileIndex[keyword]; }
		}
		
		public TileDB() {
			m_UncompiledTileDefs.Add(null); // 0 is always "nothing"
		}
		
		/// <summary>
		/// Adds a tile definition object to an uncompiled list of tiles and assigns that tile a unique ID
		/// within the database.
		/// </summary>
		/// <param name="tile">ITileDef object that contains information about the added tile</param>
		/// <returns>Newly assigned ID for a given tile</returns>
		public virtual int Add(ITileDef tile) {
			tile.ID = m_UncompiledTileDefs.Count;
			m_UncompiledTileDefs.Add(tile);
			m_TileIndex.Add(tile.Keyword, tile);
			return tile.ID;
		}
		
		/// <summary>
		/// Builds a fixed size tile definition array for faster access during the game.
		/// </summary>
		public virtual void Compile() {
			m_TileDefs = m_UncompiledTileDefs.ToArray();
			m_TileCount = m_TileDefs.Length;
			m_UncompiledTileDefs = null;
		}
		
		public virtual bool ContainsTile(string keyword) {
			return m_TileIndex.ContainsKey(keyword);
		}
	}
}
