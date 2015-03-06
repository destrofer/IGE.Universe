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

using IGE.Platform;

namespace IGE.Universe {
	public abstract class BaseGameMap : ISerializable {
		private bool m_Removed = false;
		protected double m_DeactivationTime = 0.0;
		
		/// <summary>
		/// Contains all objects in the game universe so that they could be found by their unique id.
		/// </summary>
		private Dictionary<uint, BaseGameObject> m_Objects = new Dictionary<uint, BaseGameObject>();
		
		/// <summary>
		/// Contains all active objects on the map.
		/// triggers, timers and so on.
		/// </summary>
		private List<BaseGameObject> ActiveObjects = new List<BaseGameObject>();
		
		protected Dictionary<uint, BaseGameObject> Objects { get { return m_Objects; } }
		
		public virtual string Keyword {
			get { return ""; }
		}
		
		/// <summary>
		/// This property must be initially false, and will be set to true
		/// only when the map is removed from the world.
		/// </summary>
		public bool Removed {
			get { return m_Removed; }
			internal set { m_Removed = value; }
		}
		
		public Dictionary<string, ISerializable> Meta = new Dictionary<string, ISerializable>();
		
		/// <summary>
		/// Must return global game time (GameEngine.Timer.Time) at which the map will be
		/// considered no longer active and as such will not be "thinking" anymore.
		/// </summary>
		public virtual double DeactivationTime {
			get { return m_DeactivationTime; }
			set { m_DeactivationTime = value; }
		}
		
		public IEnumerable<BaseGameObject> EnumerateObjects() {
			return m_Objects.Values;
		}

		public virtual void Remove() {
			World.RemoveMap(this);
		}
		
		internal void AddObject(BaseGameObject obj) {
			// called only from within World class so no lock is required
			// GameDebugger.Log("Map added object");
			m_Objects.Add(obj.UID, obj);
			OnObjectAddedInternal(obj);
			obj.OnMapEnterInternal();
		}
		
		internal void RemoveObject(BaseGameObject obj) {
			// called only from within World class so no lock is required
			if( m_Objects.ContainsKey(obj.UID) ) {
				// GameDebugger.Log("Map removed object");
				m_Objects.Remove(obj.UID);
				DeactivateObject(obj);
				OnObjectRemovedInternal(obj);
				obj.OnMapLeaveInternal();
			}
		}
		
		internal void ActivateObject(BaseGameObject obj) {
			// called only from within World class so no lock is required
			if( !obj.m_ObjectActive ) {
				// GameDebugger.Log("Map activated object");
				ActiveObjects.Add(obj);
				obj.m_ObjectActive = true;
				obj.OnActivateObjectInternal();
			}
		}
		
		internal void DeactivateObject(BaseGameObject obj) {
			// called only from within World class and RemoveObject method so no lock is required
			if( obj.m_ObjectActive ) {
				// GameDebugger.Log("Map deactivated object");
				ActiveObjects.Remove(obj);
				obj.m_ObjectActive = false;
				obj.OnDeactivateObjectInternal();
			}
		}
		

		internal virtual void OnObjectAddedInternal(BaseGameObject obj) {
			OnObjectAdded(obj);
		}
		
		internal virtual void OnObjectRemovedInternal(BaseGameObject obj) {
			OnObjectRemoved(obj);
		}
		
		protected virtual void OnObjectAdded(BaseGameObject obj) {
		}
		
		protected virtual void OnObjectRemoved(BaseGameObject obj) {
		}
		
		internal void ThinkInternal() {
			int i;
			BaseGameObject obj;
			
			Think();
			
			for( i = ActiveObjects.Count - 1; i >= 0; i-- ) {
				obj = ActiveObjects[i];
				if( obj == null || obj.Removed )
					ActiveObjects.RemoveAt(i);
				else
					obj.Think();
			}
		}
		
		/// <summary>
		/// Activates "thinking" process of all active objects.
		/// </summary>
		protected virtual void Think() {
			// GameDebugger.Log("Map think");
		}
		
		/// <summary>
		/// Renders the map into the current player's view.
		/// </summary>
		public virtual void Render() {
		}
		
		/// <summary>
		/// Called to clear out world specific data on world load.
		/// </summary>
		public virtual void Clear() {
			ActiveObjects.Clear();
			Objects.Clear();
			Meta.Clear();
		}
		
		public virtual void Serialize(BinaryWriter w) {
		}
		
		public virtual void Deserialize(BinaryReader r) {
		}
		
		public virtual void SerializeWorldSaveData(BinaryWriter w) {
			w.Write((int)1); // version
			
			w.Write(m_DeactivationTime);
			w.Write(Meta.Count);
			foreach( KeyValuePair<string, ISerializable> pair in Meta ) {
				w.Write(pair.Key);
				w.Write(pair.Value.GetType().FullName);
				w.Write(pair.Value);
			}
		}
		
		public virtual void DeserializeWorldSaveData(BinaryReader r) {
			string key;
			ISerializable obj;
			
			int version = r.ReadInt32();
			m_DeactivationTime = r.ReadDouble();
			int metaCount = r.ReadInt32();
			Meta.Clear();
			for( ; metaCount > 0; metaCount-- ) {
				key = r.ReadString();
				obj = (ISerializable)GlobalActivator.CreateInstance(r.ReadString());
				obj.Deserialize(r);
				Meta.Add(key, obj);
			}
		}
	}
}
