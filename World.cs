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
using System.Reflection;
using System.Globalization;

using IGE.Platform;

namespace IGE.Universe {
	public static class World {
		private static Dictionary<string, BaseGameMap> m_Maps = new Dictionary<string, BaseGameMap>();
		
		/// <summary>
		/// Contains all objects in the game universe so that they could be found by their unique id.
		/// </summary>
		private static Dictionary<uint, BaseGameObject> Objects = new Dictionary<uint, BaseGameObject>();
		
		/// <summary>
		/// Contains all objects that are not placed on any map.
		/// It is useful to store objects that must always be active like quest checkers,
		/// triggers, timers and so on.
		/// </summary>
		private static List<BaseGameObject> ActiveObjects = new List<BaseGameObject>();

		/// <summary>
		/// List of all active maps that have to "think".
		/// </summary>
		private static List<BaseGameMap> ActiveMaps = new List<BaseGameMap>();
		
		/// <summary>
		/// Contains global meta information that may be quest or something else specific.
		/// </summary>
		public static Dictionary<string, ISerializable> Meta = new Dictionary<string, ISerializable>();
		
		public static ExtRandom Rnd = new ExtRandom();
		
		public static GameTimer Time { get { return Application.Timer; } }

		private static bool m_IsSaving = false;
		public static bool IsSaving { get { return m_IsSaving; } }
		
		private static bool m_IsLoading = false;
		public static bool IsLoading { get { return m_IsLoading; } }
		
		private static uint NextUID = 0;
		
		public static uint GetNextUID() {
			unchecked {
				lock( Objects ) {
					do {
						NextUID++;
					} while( NextUID == 0 || Objects.ContainsKey(NextUID) );
					return NextUID;
				}
			}
		}
		
		public static IEnumerable<BaseGameMap> Maps { get { return m_Maps.Values; } }
		
		public static void AddMap(BaseGameMap map) {
			lock( m_Maps ) {
				if( m_Maps.ContainsKey(map.Keyword) )
					throw new UserFriendlyException("Map with such keyword is already registered", "There was an error when game tried to add a new map with an already existing keyword to the game universe");
				// GameDebugger.Log("World added map");
				m_Maps.Add(map.Keyword, map);
				ActiveMaps.Add(map); // by default newly added maps are always active in the beginning
			}
		}

		public static void RemoveMap(string mapKeyword) {
			BaseGameMap map;
			if( m_Maps.TryGetValue(mapKeyword, out map) )
				map.Remove();
		}
		
		internal static void RemoveMap(BaseGameMap map) {
			lock( m_Maps ) {
				if( m_Maps.ContainsKey(map.Keyword) ) {
					// GameDebugger.Log("World removed map");
					m_Maps.Remove(map.Keyword);
					map.Removed = true;
				}
			}
		}
		
		public static T GetMap<T>(string keyword) where T:BaseGameMap {
			return m_Maps[keyword] as T;
		}
		
		public static BaseGameMap GetMap(string keyword) {
			return m_Maps[keyword];
		}
		
		public static void AddObject(BaseGameObject obj) {
			lock( Objects ) {
				if( Objects.ContainsKey(obj.UID) )
					throw new UserFriendlyException(String.Format("Object {0} is already in the world", obj.UID), "The game tried to add same object into the world twice");
				obj.OnBeforeAddObjectInternal();
				// GameDebugger.Log("World added object");
				Objects.Add(obj.UID, obj);
				if( obj.BaseGameMap != null )
					obj.BaseGameMap.AddObject(obj);
				obj.OnAfterAddObjectInternal();
			}
		}
		
		public static void ActivateObject(BaseGameObject obj) {
			lock( Objects ) {
				if( Objects.ContainsKey(obj.UID) ) {
					if( obj.BaseGameMap != null )
						obj.BaseGameMap.ActivateObject(obj);
					else if( !obj.m_ObjectActive ) {
						// GameDebugger.Log("World activated object");
						ActiveObjects.Add(obj);
						obj.m_ObjectActive = true;
						obj.OnActivateObjectInternal();
					}
				}
				// else
					// throw new UserFriendlyException(String.Format("Cannot activate object {0}: it is not in the world", obj.UID), "The game tried to activate an object that was not added into the world");
			}
		}
		
		public static void DeactivateObject(BaseGameObject obj) {
			lock( Objects ) {
				if( Objects.ContainsKey(obj.UID) ) {
					if( obj.BaseGameMap != null )
						obj.BaseGameMap.DeactivateObject(obj);
					else if( obj.m_ObjectActive ) {
						// GameDebugger.Log("World deactivated object");
						ActiveObjects.Remove(obj);
						obj.m_ObjectActive = false;
						obj.OnDeactivateObjectInternal();
					}
				}
				// else
					// throw new UserFriendlyException(String.Format("Cannot deactivate object {0}: it is not added to the world yet", obj.UID), "The game tried to deactivate that was not added into the world");
			}
		}
		
		public static void RemoveObject(BaseGameObject obj) {
			lock( Objects ) {
				if( Objects.ContainsKey(obj.UID) ) {
					obj.OnBeforeRemoveObjectInternal();
					// GameDebugger.Log("World removed object");
					Objects.Remove(obj.UID);
					BaseGameMap map = obj.BaseGameMap;
					if( map != null )
						map.RemoveObject(obj);
					else if( obj.m_ObjectActive ) {
						// GameDebugger.Log("World deactivated object (on remove)");
						ActiveObjects.Remove(obj);
						obj.m_ObjectActive = false;
						obj.OnDeactivateObjectInternal();
					}
					obj.OnAfterRemoveObjectInternal();
				}
			}
		}
		
		internal static void MoveObjectAcrossMaps(BaseGameObject obj, BaseGameMap map) {
			lock( Objects ) {
				if( obj.BaseGameMap != map ) {
					if( Objects.ContainsKey(obj.UID) ) {
						// GameDebugger.Log("World moved object across maps");
						bool wasActive = obj.m_ObjectActive;
						if( obj.m_BaseGameMap != null )
							obj.m_BaseGameMap.RemoveObject(obj);
						else if( wasActive ) {
							// GameDebugger.Log("World deactivated object (moam)");
							ActiveObjects.Remove(obj);
							obj.m_ObjectActive = false;
							obj.OnDeactivateObjectInternal();
						}
						
						obj.m_BaseGameMap = map;
						
						if( map != null ) {
							map.AddObject(obj);
							if( wasActive )
								map.ActivateObject(obj);
						}
						else if( wasActive ) {
							// GameDebugger.Log("World activated object (moam)");
							ActiveObjects.Add(obj);
							obj.m_ObjectActive = true;
							obj.OnActivateObjectInternal();
						}
					}
					else {
						// GameDebugger.Log("World updated object map");
						obj.m_BaseGameMap = map;
					}
				}
			}
		}
		
		public static BaseGameObject GetObject(uint uid) {
			return Objects[uid];
		}
		
		public static void Clear() {
			NextUID = 0;
			Objects = new Dictionary<uint, BaseGameObject>();
			ActiveObjects = new List<BaseGameObject>();
			Meta = new Dictionary<string, ISerializable>();
			Rnd = new ExtRandom();
			Time.Reset();
			foreach( BaseGameMap map in Maps )
				map.Clear();
		}
		
		public static void Save(string path) {
			int index;
			Dictionary<string, int> mapIndex = new Dictionary<string, int>();
			HashSet<BaseGameMap> activeMapIndex = new HashSet<BaseGameMap>(ActiveMaps); // hashset will work faster than list to find maps
			Dictionary<string, int> classIndex = new Dictionary<string, int>();
			List<string> classList = new List<string>(); // using list, because dictionary doesn't necessarily keep order
			string kwd;
			
			m_IsSaving = true;
			try {
				lock(Objects) {
					index = 0;
					foreach( BaseGameObject obj in Objects.Values ) {
						if( !classIndex.ContainsKey(kwd = obj.GetType().FullName) ) {
							classIndex.Add(kwd, index++);
							classList.Add(kwd);
						}
						obj.OnBeforeSaveInternal();
					}
					
					using(BinaryWriter w = new BinaryWriter(new FileStream(path, FileMode.Create))) {
						w.Write((int)0x1F3EA7D2); // world save file identifier 
						w.Write((int)1); // world version
	
						w.Write(Time);
						w.Write(Rnd);
						// w.Write(NextUID);
						
						// save world specific meta information
						w.Write(Meta.Count);
						foreach( KeyValuePair<string, ISerializable> pair in Meta ) {
							w.Write(pair.Key);
							w.Write(pair.Value.GetType().FullName);
							w.Write(pair.Value);
						}
						
						// save map specific information and map index
						index = 1;
						w.Write(m_Maps.Count);
						foreach( BaseGameMap map in m_Maps.Values ) {
							mapIndex.Add(map.Keyword, index++);
							w.Write(map.Keyword);
							w.Write(activeMapIndex.Contains(map));
							map.SerializeWorldSaveData(w);
						}
						
						// save object class index
						w.Write(classList.Count);
						foreach( string className in classList )
							w.Write(className);
						
						// save object index for precreation
						w.Write(Objects.Count);
						foreach( BaseGameObject obj in Objects.Values ) {
							w.Write(obj.UID);
							w.Write(classIndex[obj.GetType().FullName]);
							if( obj.BaseGameMap != null )
								w.Write(mapIndex[obj.BaseGameMap.Keyword]);
							else
								w.Write((int)0);
						}
						
						// save objects
						foreach( BaseGameObject obj in Objects.Values ) {
							w.Write(obj.UID);
							w.Write(obj.ObjectActive);
							w.Write(obj);
						}
					}
					foreach( BaseGameObject obj in Objects.Values )
						obj.OnAfterSaveInternal();
				}
			}
			finally {
				m_IsSaving = false;
			}
		}
		
		public static void Load(string path) {
			int count, index, objMap;
			string key;
			ISerializable metaObj;
			bool isActive;
			uint uid;
			List<BaseGameMap> mapIndex = new List<BaseGameMap>();
			List<string> classIndex = new List<string>();
			BaseGameObject obj;
			BaseGameMap map;
			
			m_IsLoading = true;
			try {
				using(BinaryReader r = new BinaryReader(new FileStream(path, FileMode.Open))) {
					lock(Objects) {
						if( r.ReadInt32() != 0x1F3EA7D2 )
							throw new UserFriendlyException("World save file is either corupt or is of unsupported format", "Failed to load world");
						
						Clear();
						// ActiveMaps = new List<BaseGameMap>(); // map activation is not implemented yet
						
						int version = r.ReadInt32();
						Time.Deserialize(r);
						Rnd.Deserialize(r);
						// NextUID = r.ReadUInt32();
						
						// load world meta data
						count = r.ReadInt32();
						for( ; count > 0; count-- ) {
							key = r.ReadString();
							metaObj = (ISerializable)GlobalActivator.CreateInstance(r.ReadString());
							metaObj.Deserialize(r);
							Meta.Add(key, metaObj);
						}
						
						// load map specific data and map index
						// this will crash if any map changed class or was removed from the map database.
						count = r.ReadInt32();
						for( ; count > 0; count-- ) {
							key = r.ReadString();
							isActive = r.ReadBoolean();
							map = m_Maps[key];
							mapIndex.Add(map);
							// if( isActive )
								// ActiveMaps.Add(map); // we might need to deactivate since new maps that are not in save will get deactivated because array is recreated 
							map.DeserializeWorldSaveData(r);
						}
						
						// load object class index
						count = r.ReadInt32();
						for( ; count > 0; count-- ) {
							key = r.ReadString();
							classIndex.Add(key);
							// GameDebugger.Log("{0}", key);
						}
						
						// load object index
						count = r.ReadInt32();
						// GameDebugger.Log("Objects: {0}", count);
						for( ; count > 0; count-- ) {
							uid = r.ReadUInt32();
							index = r.ReadInt32();
							objMap = r.ReadInt32();
							obj = (BaseGameObject)GlobalActivator.CreateInstance(classIndex[index], uid);
							obj.OnBeforeLoadInternal();
							obj.m_BaseGameMap = (objMap == 0) ? null : mapIndex[objMap - 1];
							AddObject(obj);
						}
						
						count = Objects.Count;
						for( ; count > 0; count-- ) {
							uid = r.ReadUInt32();
							isActive = r.ReadBoolean();
							obj = Objects[uid];
							obj.Deserialize(r);
							if( isActive )
								ActivateObject(obj);
						}
					}
				}
				
				foreach( BaseGameObject eobj in Objects.Values )
					eobj.OnAfterLoadInternal();
			}
			finally {
				m_IsLoading = false;
			}
		}

		public static void Think() {
			int i;
			BaseGameMap map;
			BaseGameObject obj;
			lock( Objects ) { // adding and removing objects from other threads is disallowed while "thinking" is in progress
				for( i = ActiveMaps.Count - 1; i >= 0; i-- ) {
					map = ActiveMaps[i];
					if( map == null || map.Removed || Application.Timer.Time > map.DeactivationTime && map.DeactivationTime > 0 )
						ActiveMaps.RemoveAt(i);
					else
						map.ThinkInternal();
				}
				
				for( i = ActiveObjects.Count - 1; i >= 0; i-- ) {
					obj = ActiveObjects[i];
					if( obj == null || obj.Removed ) // this should never happen, but who knows ...
						ActiveObjects.RemoveAt(i);
					else
						obj.Think();
				}
			}
		}
		
		public static T FindObject<T>() where T : BaseGameObject {
			foreach( BaseGameObject obj in Objects.Values )
				if( obj is T )
					return (T)obj;
			return null;
		}
		
		static World() {
		}
	}
}
