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

namespace IGE.Universe {
	public abstract class BaseGameObject : ISerializable {
		private uint m_UID;
		public uint UID { get { return m_UID; } }
		
		private bool m_Removed = false;
		public bool Removed {
			get { return m_Removed; }
		}
		
		internal BaseGameMap m_BaseGameMap = null;
		public BaseGameMap BaseGameMap {
			get { return m_BaseGameMap; }
			set { World.MoveObjectAcrossMaps(this, value); }
		}
		
		internal bool m_ObjectActive = false;
		public bool ObjectActive {
			get { return m_ObjectActive; }
			set {
				if( value )
					World.ActivateObject(this);
				else
					World.DeactivateObject(this);
			}
		}
		
		public BaseGameObject() {
			m_UID = World.GetNextUID();
		}
		
		public BaseGameObject(uint uid) {
			m_UID = uid;
		}
		
		public void RemoveObject() {
			World.RemoveObject(this);
		}
				
		internal void OnBeforeAddObjectInternal() {
			// GameDebugger.Log("Object OnBeforeAddObjectInternal");
			OnBeforeAddObject();
			m_Removed = false;
		}
		
		internal void OnAfterAddObjectInternal() {
			// GameDebugger.Log("Object OnAfterAddObjectInternal");
			OnAfterAddObject();
		}

		internal void OnBeforeRemoveObjectInternal() {
			// GameDebugger.Log("Object OnBeforeRemoveObjectInternal");
			OnBeforeRemoveObject();
		}
		
		internal void OnAfterRemoveObjectInternal() {
			// GameDebugger.Log("Object OnAfterRemoveObjectInternal");
			m_Removed = true;
			OnAfterRemoveObject();
		}
		
		internal void OnActivateObjectInternal() {
			// GameDebugger.Log("Object OnActivateObjectInternal");
			OnActivateObject();
		}
		
		internal void OnDeactivateObjectInternal() {
			// GameDebugger.Log("Object OnDeactivateObjectInternal");
			OnDeactivateObject();
		}
		
		internal void OnMapEnterInternal() {
			OnMapEnter();
		}
		
		internal void OnMapLeaveInternal() {
			OnMapLeave();
		}
		
		/// <summary>
		/// This method is called before adding the object to the world. 
		/// </summary>
		protected virtual void OnBeforeAddObject() {
		}
		
		/// <summary>
		/// This method is called after object is added to the world. 
		/// </summary>
		protected virtual void OnAfterAddObject() {
		}
		
		/// <summary>
		/// This method is called when the object is about to be removed from the world.
		/// </summary>
		protected virtual void OnBeforeRemoveObject() {
		}
		
		/// <summary>
		/// This method is called after the object has beed removed from the world.
		/// </summary>
		protected virtual void OnAfterRemoveObject() {
		}
		
		/// <summary>
		/// This method is called whenever object becomes active (gets added to world or map think list)
		/// </summary>
		protected virtual void OnActivateObject() {
		}
		
		/// <summary>
		/// This method is called whenever object becomes inactive (gets removed from world or map think list)
		/// </summary>
		protected virtual void OnDeactivateObject() {
		}
		
		/// <summary>
		/// This method is called after object was added to a map. 
		/// </summary>
		protected virtual void OnMapEnter() {
		}

		/// <summary>
		/// This method is called after object was removed from a map. 
		/// </summary>
		protected virtual void OnMapLeave() {
		}
		
		internal virtual void OnBeforeSaveInternal() {
			OnBeforeSave();
		}
		
		internal virtual void OnAfterSaveInternal() {
			OnAfterSave();
		}
		
		internal virtual void OnBeforeLoadInternal() {
			OnBeforeLoad();
		}

		internal virtual void OnAfterLoadInternal() {
			OnAfterLoad();
		}
		
		/// <summary>
		/// All objects in world get that method called right before saving process starts
		/// </summary>
		protected virtual void OnBeforeSave() {
		}
		
		/// <summary>
		/// All objects in world get that method called after saving process finishes
		/// </summary>
		protected virtual void OnAfterSave() {
		}
		
		/// <summary>
		/// All objects in world get that method called when they were created from the saved index, but before their data is getting loaded (before calling Deserialize() method)
		/// </summary>
		protected virtual void OnBeforeLoad() {
		}

		/// <summary>
		///  All objects in world get that method called after all objects of the world are loaded
		/// </summary>
		protected virtual void OnAfterLoad() {
		}
		
		public virtual void Think() {
			// GameDebugger.Log("Object think");
		}
		
		public virtual void Serialize(BinaryWriter w) {
		}
		
		public virtual void Deserialize(BinaryReader r) {
		}
		
		public virtual void Render() {
		}
	}
}
