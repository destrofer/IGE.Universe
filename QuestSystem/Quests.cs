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

using IGE.Data.Articy;
using IGE.Scripting;

namespace IGE.Universe.QuestSystem {
	/// <summary>
	/// Description of Quests.
	/// </summary>
	public static class Quests {
		public static readonly QuestConditionScriptEnvironment ConditionScriptEnvironment = new QuestConditionScriptEnvironment();
		public static readonly QuestInstructionScriptEnvironment InstructionScriptEnvironment = new QuestInstructionScriptEnvironment();
		public static readonly QuestInputPinScriptEnvironment InputPinScriptEnvironment = new QuestInputPinScriptEnvironment();
		public static readonly QuestOutputPinScriptEnvironment OutputPinScriptEnvironment = new QuestOutputPinScriptEnvironment();
	
		private static readonly Dictionary<ulong, BaseQuestObject> m_Objects = new Dictionary<ulong, BaseQuestObject>();
		private static readonly Dictionary<string, BaseQuestObject> m_ObjectIndex = new Dictionary<string, BaseQuestObject>();
		private static readonly Dictionary<ulong, Quest> m_Quests = new Dictionary<ulong, Quest>();
		private static readonly Dictionary<string, Quest> m_QuestIndex = new Dictionary<string, Quest>();
		private static readonly List<QuestPin> m_InitiallyActiveQuestPins = new List<QuestPin>();
		
		static Quests() {
		}

		public static IList<QuestPin> GetInitialPins() {
			return m_InitiallyActiveQuestPins.AsReadOnly();
		}
		
		public static Quest GetQuest(ulong id) {
			return m_Quests[id];
		}

		public static Quest GetQuest(string key) {
			return m_QuestIndex[key];
		}

		public static BaseQuestObject GetObject(ulong id) {
			return m_Objects[id];
		}

		public static BaseQuestObject GetObject(string key) {
			return m_ObjectIndex[key];
		}
		
		public static void Add(BaseQuestObject obj) {
			m_Objects.Add(obj.Id, obj);
			if( obj is Quest )
				m_Quests.Add(obj.Id, (Quest)obj);
			if( !string.IsNullOrWhiteSpace(obj.Key) ) {
				GameDebugger.Log(LogLevel.Debug, "Added {0} '{1}' to quest system", obj.GetType().Name, obj.Key);
				m_ObjectIndex.Add(obj.Key, obj);
				if( obj is Quest )
					m_QuestIndex.Add(obj.Key, (Quest)obj);
			}
			else
				GameDebugger.Log(LogLevel.Debug, "Added {0} '0x{1:X16}' to quest system", obj.GetType().Name, obj.Id);
		}
		
		private static Script CompileScript(string code, QuestSystemScriptEnvironment environment, string type, ulong id) {
			GameDebugger.Log(LogLevel.Debug, "Compiling script for {0} '0x{1:X16}'", type, id);
			Script script = new Script(code);
			script.Environment = environment;
			ErrorInfo[] errors = script.Compile();
			bool badScript = false;
			
			if( errors.Length > 0 ) {
				foreach( ErrorInfo error in errors ) {
					if( error.Level >= ErrorLevel.Error )
						badScript = true;
					GameDebugger.Log(error.LogLevel, "{0} '0x{1:X16}' script: [{2}] {3}", type, id, error.Level, error.Message);
				}
			}
			
			if( badScript ) {
				GameDebugger.Log(LogLevel.Debug, "Script compilation failed");
				return null;
			}
			GameDebugger.Log(LogLevel.Debug, "Script compiled successfully");
			return script;
		}
		
		private static bool ImportArticyPins(ArticyFlowObject source, BaseQuestObject target, bool compileInputScripts, bool compileOutputScripts, Dictionary<ulong, QuestPin> pinIndex) {
			FlowObjectPin articyPin;
			QuestPin pin;
			bool badScripts = false;
			
			foreach( GraphPin graphPin in source.Inputs ) {
				articyPin = (FlowObjectPin)graphPin;
				pin = new QuestPin(articyPin.Type, articyPin.Id);
				if( compileInputScripts && !string.IsNullOrWhiteSpace(articyPin.Script) ) {
					if( (pin.Script = CompileScript(articyPin.Script, Quests.InputPinScriptEnvironment, "input pin", articyPin.Id)) == null )
					   badScripts = true;
				}
				if( string.IsNullOrEmpty(target.Key) )
					GameDebugger.Log(LogLevel.Debug, "Adding intput pin to '0x{0:X16}'", target.Id);
				else
					GameDebugger.Log(LogLevel.Debug, "Adding intput pin to '{0}'", target.Key);
				pinIndex.Add(articyPin.Id, pin);
				target.AddInput(pin);
			}
			
			foreach( GraphPin graphPin in source.Outputs ) {
				articyPin = (FlowObjectPin)graphPin;
				pin = new QuestPin(articyPin.Type, articyPin.Id);
				if( compileOutputScripts && !string.IsNullOrWhiteSpace(articyPin.Script) ) {
					QuestSystemScriptEnvironment env;
					env = (target is QuestObjective) ? (QuestSystemScriptEnvironment)Quests.OutputPinScriptEnvironment : (QuestSystemScriptEnvironment)Quests.InstructionScriptEnvironment;
					if( (pin.Script = CompileScript(articyPin.Script, env, "output pin", articyPin.Id)) == null )
					   badScripts = true;
				}
				if( string.IsNullOrEmpty(target.Key) )
					GameDebugger.Log(LogLevel.Debug, "Adding output pin to '0x{0:X16}'", target.Id);
				else
					GameDebugger.Log(LogLevel.Debug, "Adding output pin to '{0}'", target.Key);
				pinIndex.Add(articyPin.Id, pin);
				target.AddOutput(pin);
			}
			
			return badScripts;
		}
		
		public static bool ImportArticyQuests(Project project, string questContainerExternalId) {
			ArticyFlowObject questContainer;
			try {
				questContainer = project.GetFlowObject(questContainerExternalId);
			}
			catch {
				throw new UserFriendlyException(
					String.Format("Could not find quest container '{0}' in the project.", questContainerExternalId),
					"One of data files does not contain required information."
				);
			}
			
			IList<GraphNode> quests = questContainer.Children;
			GameDebugger.Log(LogLevel.Debug, "{0} quests found:", quests.Count);
			
			ArticyFlowFragment articyQuest;
			Quest quest;
			Dictionary<ulong, QuestPin> pinIndex = new Dictionary<ulong, QuestPin>();
			List<Jump> articyJumps = new List<Jump>();
			bool badScripts = false;
			
			// copy quests
			foreach( GraphNode node in questContainer.Children ) {
				// hubs are used to join quests for further story progression: hub remains inactive until all quests connected to the hub are complete
				if( node is ArticyHub ) {
					ArticyHub articyHub = (ArticyHub)node;
					QuestHub hub = new QuestHub(articyHub.Id, articyHub.ExternalId);
					badScripts |= ImportArticyPins(articyHub, hub, true, true, pinIndex);
					GameDebugger.Log(LogLevel.Debug, "Adding quest hub '0x{0:X16}'", hub.Id);
					Add(hub);
				}
				
				// flow fragments are quests
				if( node is ArticyFlowFragment ) {
					articyQuest = (ArticyFlowFragment)node;
					quest = new Quest(articyQuest.Id, articyQuest.ExternalId);

					GameDebugger.Log(LogLevel.Debug, "Importing quest '{0}'", quest.Key);
					GameDebugger.Log(LogLevel.Debug, "Quest has {0} inputs and {1} outputs", articyQuest.Inputs.Count, articyQuest.Outputs.Count);
					
					badScripts |= ImportArticyPins(articyQuest, quest, true, true, pinIndex);
					
					quest.Name = articyQuest.DisplayName;
					quest.Description = articyQuest.Text;
					
					Add(quest);
					
					// copy quest fragments
					foreach( GraphNode childNode in node.Children ) {
						if( childNode is ArticyDialogue || childNode is ArticyDialogueFragment )
							throw new UserFriendlyException(
								String.Format("Dialogue or dialogue fragment '0x{0:X16}' is not supposed to be a part of a quest", ((ArticyFlowFragment)childNode).Id),
								"One of data files is either corupt or has a version that is not supported by the game engine."
							);
						
						if( childNode is ArticyFlowFragment ) {
							ArticyFlowFragment articyFragment = (ArticyFlowFragment)childNode;
							if( string.IsNullOrWhiteSpace(articyFragment.DisplayName) ) {
								QuestLogicGate gate = new QuestLogicGate(articyFragment.Id, articyFragment.ExternalId);
								badScripts |= ImportArticyPins(articyFragment, gate, true, true, pinIndex);
								GameDebugger.Log(LogLevel.Debug, "Adding logic gate '0x{0:X16}' to the quest", gate.Id);
								quest.AddChild(gate);
								Add(gate);
							}
							else {
								QuestObjective objective = new QuestObjective(articyFragment.Id, articyFragment.ExternalId);
								objective.Description = articyFragment.Text;
								badScripts |= ImportArticyPins(articyFragment, objective, true, true, pinIndex);
								GameDebugger.Log(LogLevel.Debug, "Adding objective '0x{0:X16}' to the quest", objective.Id);
								objective.Name = articyFragment.DisplayName;
								objective.Description = articyFragment.Text;
								quest.AddChild(objective);
								Add(objective);
							}
						}
						
						if( childNode is ArticyCondition ) {
							ArticyCondition articyCondition = (ArticyCondition)childNode;
							QuestCondition condition = new QuestCondition(articyCondition.Id, articyCondition.ExternalId);
							if( string.IsNullOrWhiteSpace(articyCondition.Script) ) {
								badScripts = true;
								GameDebugger.Log(LogLevel.Error, "Condition '0x{0:X16}' has no script", condition.Id);
							}
							else {
								if( (condition.Script = CompileScript(articyCondition.Script, Quests.ConditionScriptEnvironment, "condition", condition.Id)) == null )
									badScripts = true;
							}
							badScripts |= ImportArticyPins(articyCondition, condition, true, true, pinIndex);
							GameDebugger.Log(LogLevel.Debug, "Adding condition '0x{0:X16}' to the quest", condition.Id);
							quest.AddChild(condition);
							Add(condition);
						}
						
						if( childNode is ArticyInstruction ) {
							ArticyInstruction articyInstruction = (ArticyInstruction)childNode;
							QuestInstruction instruction = new QuestInstruction(articyInstruction.Id, articyInstruction.ExternalId);
							if( string.IsNullOrWhiteSpace(articyInstruction.Script) ) {
								GameDebugger.Log(LogLevel.Notice, "Instruction '0x{0:X16}' has no script", instruction.Id);
							}
							else {
								if( (instruction.Script = CompileScript(articyInstruction.Script, Quests.ConditionScriptEnvironment, "instruction", instruction.Id)) == null )
									badScripts = true;
							}
							badScripts |= ImportArticyPins(articyInstruction, instruction, true, true, pinIndex);
							GameDebugger.Log(LogLevel.Debug, "Adding instruction '0x{0:X16}' to the quest", instruction.Id);
							quest.AddChild(instruction);
							Add(instruction);
						}
						
						if( childNode is ArticyHub ) {
							ArticyHub articyHub = (ArticyHub)childNode;
							QuestSavePoint savePoint = new QuestSavePoint(articyHub.Id, articyHub.ExternalId);
							badScripts |= ImportArticyPins(articyHub, savePoint, true, true, pinIndex);
							GameDebugger.Log(LogLevel.Debug, "Adding save point '0x{0:X16}' to the quest", savePoint.Id);
							quest.AddChild(savePoint);
							Add(savePoint);
						}
						
						if( childNode is Jump ) {
							articyJumps.Add((Jump)childNode);
						}
					}
				}
			}
			
			// jumps need postprocessing, because we must make sure all quest system nodes are already added
			// jumps must be treated as connections. they must be disallowed to connect to anything outside the scope of current quest.
			BaseQuestObject obj;
			QuestPin srcPin, dstPin;
			IList<GraphPin> inputs;
			foreach( Jump articyJump in articyJumps ) {
				GameDebugger.Log(LogLevel.Debug, "Linking jump '0x{0:X16}'", articyJump.Id);
				
				if( articyJump.TargetPin != null ) {
					if( !pinIndex.TryGetValue(articyJump.TargetPin.Id, out dstPin) ) {
						GameDebugger.Log(LogLevel.Debug, "Ignoring jump '0x{0:X16}' since it's target pin is outside the quest container", articyJump.Id);
						continue;
					}
				}
				else if( articyJump.Target != null ) {
					inputs = articyJump.Target.Inputs;
					if( inputs.Count == 0 ) {
						GameDebugger.Log(LogLevel.Debug, "Ignoring jump '0x{0:X16}' since it's target does not have input pins", articyJump.Id);
						continue;
					}
					if( !pinIndex.TryGetValue(((FlowObjectPin)inputs[0]).Id, out dstPin) ) {
						GameDebugger.Log(LogLevel.Debug, "Ignoring jump '0x{0:X16}' since it's target is outside the quest container", articyJump.Id);
						continue;
					}
				}
				else
					continue;
				
				obj = (BaseQuestObject)dstPin.Node;
				if( obj.Parent == null || !(obj.Parent is Quest) || ((Quest)obj.Parent).Id != ((ArticyFlowObject)articyJump.Parent).Id ) {
					GameDebugger.Log(LogLevel.Debug, "Ignoring jump '0x{0:X16}' since it's not targetting an object within the same quest", articyJump.Id);
					continue;
				}
				
				foreach( FlowObjectPin pin in articyJump.Inputs ) {
					foreach( ArticyFlowConnection articyConnection in pin.Connections ) {
						if( articyConnection.Target != pin )
							continue;
						if( !pinIndex.TryGetValue(((FlowObjectPin)articyConnection.Source).Id, out srcPin) )
							continue;
						QuestConnection newConnection = new QuestConnection(articyConnection.Id, articyConnection.ExternalId);
						newConnection.Source = srcPin;
						newConnection.Target = dstPin;
						GameDebugger.Log(LogLevel.Debug, "Connected '0x{0:X16}' to '0x{1:X16}' (jump replacement)", ((BaseQuestObject)newConnection.Source.Node).Id, ((BaseQuestObject)newConnection.Target.Node).Id);
					}
				}
			}
			
			// recreate connections
			foreach( ArticyFlowConnection articyConnection in project.FlowConnections ) {
				if( !pinIndex.TryGetValue(((FlowObjectPin)articyConnection.Target).Id, out dstPin)  )
					continue;
				
				if( dstPin.Type == GraphPin.PinType.Input && dstPin.Node is Quest && articyConnection.Source.Node == questContainer ) {
					if( !m_InitiallyActiveQuestPins.Contains(dstPin) )
						m_InitiallyActiveQuestPins.Add(dstPin);
					GameDebugger.Log(LogLevel.Debug, "Adding pin '0x{0:X16}' to list of initially active quest pins", dstPin.Id);
					continue;
				}
				
				if( !pinIndex.TryGetValue(((FlowObjectPin)articyConnection.Source).Id, out srcPin) )
					continue;

				QuestConnection newConnection = new QuestConnection(articyConnection.Id, articyConnection.ExternalId);
				newConnection.Source = srcPin;
				newConnection.Target = dstPin;
				
				GameDebugger.Log(LogLevel.Debug, "Connected '0x{0:X16}' to '0x{1:X16}'", ((BaseQuestObject)newConnection.Source.Node).Id, ((BaseQuestObject)newConnection.Target.Node).Id);
			}
			
			return badScripts;
		}
		
	}
}
