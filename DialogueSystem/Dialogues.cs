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

using IGE;
using IGE.Scripting;
using IGE.Data.Articy;

namespace IGE.Universe.DialogueSystem {
	/// <summary>
	/// Description of Dialogues.
	/// </summary>
	public static class Dialogues {
		public static readonly DialogueConditionScriptEnvironment ConditionScriptEnvironment = new DialogueConditionScriptEnvironment();
		public static readonly DialogueInstructionScriptEnvironment InstructionScriptEnvironment = new DialogueInstructionScriptEnvironment();
		public static readonly DialogueInputPinScriptEnvironment InputPinScriptEnvironment = new DialogueInputPinScriptEnvironment();
		
		private static readonly Dictionary<ulong, BaseDialogueObject> m_Objects = new Dictionary<ulong, BaseDialogueObject>();
		private static readonly Dictionary<string, BaseDialogueObject> m_ObjectIndex = new Dictionary<string, BaseDialogueObject>();
		private static readonly Dictionary<ulong, Dialogue> m_Dialogues = new Dictionary<ulong, Dialogue>();
		private static readonly Dictionary<string, Dialogue> m_DialogueIndex = new Dictionary<string, Dialogue>();
		
		static Dialogues() {
		}
		
		public static BaseDialogueObject GetObject(ulong id) {
			return m_Objects[id];
		}

		public static BaseDialogueObject GetObject(string key) {
			return m_ObjectIndex[key];
		}
		
		public static Dialogue GetDialogue(ulong id) {
			return m_Dialogues[id];
		}

		public static Dialogue GetDialogue(string key) {
			return m_DialogueIndex[key];
		}
		
		public static void Add(BaseDialogueObject obj) {
			m_Objects.Add(obj.Id, obj);
			if( obj is Dialogue )
				m_Dialogues.Add(obj.Id, (Dialogue)obj);
			if( !string.IsNullOrWhiteSpace(obj.Key) ) {
				GameDebugger.Log(LogLevel.Debug, "Added {0} '{1}' to dialogue system", obj.GetType().Name, obj.Key);
				m_ObjectIndex.Add(obj.Key, obj);
				if( obj is Dialogue )
					m_DialogueIndex.Add(obj.Key, (Dialogue)obj);
			}
			else
				GameDebugger.Log(LogLevel.Debug, "Added {0} '0x{1:X16}' to dialogue system", obj.GetType().Name, obj.Id);
		}
		
		private static Script CompileScript(string code, DialogueSystemScriptEnvironment environment, string type, ulong id) {
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
		
		private static bool ImportArticyPins(ArticyFlowObject source, BaseDialogueObject target, bool compileInputScripts, bool compileOutputScripts, Dictionary<ulong, DialoguePin> pinIndex) {
			FlowObjectPin articyPin;
			DialoguePin pin;
			bool badScripts = false;
			
			foreach( GraphPin graphPin in source.Inputs ) {
				articyPin = (FlowObjectPin)graphPin;
				pin = new DialoguePin(articyPin.Type);
				if( compileInputScripts && !string.IsNullOrWhiteSpace(articyPin.Script) ) {
					if( (pin.Script = CompileScript(articyPin.Script, Dialogues.InputPinScriptEnvironment, "input pin", articyPin.Id)) == null )
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
				pin = new DialoguePin(articyPin.Type);
				if( compileOutputScripts && !string.IsNullOrWhiteSpace(articyPin.Script) ) {
					if( (pin.Script = CompileScript(articyPin.Script, Dialogues.InstructionScriptEnvironment, "output pin", articyPin.Id)) == null )
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
		
		public static bool ImportArticyDialogues(Project project, string dialogueContainerExternalId) {
			ArticyFlowObject dialogueContainer;
			
			try {
				dialogueContainer = project.GetFlowObject(dialogueContainerExternalId);
			}
			catch {
				throw new UserFriendlyException(
					String.Format("Could not find dialogue container '{0}' in the project.", dialogueContainerExternalId),
					"One of data files does not contain required information."
				);
			}
			
			IList<GraphNode> dialogues = dialogueContainer.Children;
			GameDebugger.Log(LogLevel.Debug, "{0} dialogues found:", dialogues.Count);
			
			ArticyDialogue articyDialogue;
			Dialogue dialogue;
			Dictionary<ulong, DialoguePin> pinIndex = new Dictionary<ulong, DialoguePin>();
			List<Jump> articyJumps = new List<Jump>();
			DialogueJump jump;
			bool badScripts = false;
			
			// copy dialogues
			foreach( GraphNode node in dialogueContainer.Children ) {
				if( !(node is ArticyDialogue) )
					continue;
				articyDialogue = (ArticyDialogue)node;
				dialogue = new Dialogue(articyDialogue.Id, articyDialogue.ExternalId);

				GameDebugger.Log(LogLevel.Debug, "Importing dialogue '{0}'", dialogue.Key);
				GameDebugger.Log(LogLevel.Debug, "Dialogue has {0} inputs and {1} outputs", articyDialogue.Inputs.Count, articyDialogue.Outputs.Count);
				
				badScripts |= ImportArticyPins(articyDialogue, dialogue, false, false, pinIndex);
				
				Add(dialogue);
				
				// copy dialogue fragments
				foreach( GraphNode childNode in node.Children ) {
					if( childNode is ArticyFlowFragment )
						throw new UserFriendlyException(
							String.Format("FlowFragment '0x{0:X16}' is not supposed to be a part of a dialogue", ((ArticyFlowFragment)childNode).Id),
							"One of data files is either corupt or has a version that is not supported by the game engine."
						);
					
					if( childNode is ArticyDialogueFragment ) {
						ArticyDialogueFragment articyFragment = (ArticyDialogueFragment)childNode;
						DialogueFragment fragment = new DialogueFragment(articyFragment.Id, articyFragment.ExternalId);
						fragment.ActorId = (articyFragment.Speaker == null) ? 0UL : articyFragment.Speaker.Id;
						fragment.MenuText = articyFragment.MenuText;
						fragment.Text = articyFragment.Text;
						fragment.Extra = articyFragment.StageDirections;
						badScripts |= ImportArticyPins(articyFragment, fragment, true, true, pinIndex);
						GameDebugger.Log(LogLevel.Debug, "Adding fragment '0x{0:X16}' to the dialogue", fragment.Id);
						dialogue.AddChild(fragment);
						Add(fragment);
					}
					
					if( childNode is ArticyCondition ) {
						ArticyCondition articyCondition = (ArticyCondition)childNode;
						DialogueCondition condition = new DialogueCondition(articyCondition.Id, articyCondition.ExternalId);
						if( string.IsNullOrWhiteSpace(articyCondition.Script) ) {
							badScripts = true;
							GameDebugger.Log(LogLevel.Error, "Condition '0x{0:X16}' has no script", condition.Id);
						}
						else {
							if( (condition.Script = CompileScript(articyCondition.Script, Dialogues.ConditionScriptEnvironment, "condition", condition.Id)) == null )
								badScripts = true;
						}
						badScripts |= ImportArticyPins(articyCondition, condition, true, true, pinIndex);
						GameDebugger.Log(LogLevel.Debug, "Adding condition '0x{0:X16}' to the dialogue", condition.Id);
						dialogue.AddChild(condition);
						Add(condition);
					}
					
					if( childNode is ArticyInstruction ) {
						ArticyInstruction articyInstruction = (ArticyInstruction)childNode;
						DialogueInstruction instruction = new DialogueInstruction(articyInstruction.Id, articyInstruction.ExternalId);
						if( string.IsNullOrWhiteSpace(articyInstruction.Script) ) {
							GameDebugger.Log(LogLevel.Notice, "Instruction '0x{0:X16}' has no script", instruction.Id);
						}
						else {
							if( (instruction.Script = CompileScript(articyInstruction.Script, Dialogues.ConditionScriptEnvironment, "instruction", instruction.Id)) == null )
								badScripts = true;
						}
						badScripts |= ImportArticyPins(articyInstruction, instruction, true, true, pinIndex);
						GameDebugger.Log(LogLevel.Debug, "Adding instruction '0x{0:X16}' to the dialogue", instruction.Id);
						dialogue.AddChild(instruction);
						Add(instruction);
					}
					
					if( childNode is ArticyHub ) {
						ArticyHub articyHub = (ArticyHub)childNode;
						DialogueHub hub = new DialogueHub(articyHub.Id, articyHub.ExternalId);
						badScripts |= ImportArticyPins(articyHub, hub, true, true, pinIndex);
						GameDebugger.Log(LogLevel.Debug, "Adding hub '0x{0:X16}' to the dialogue", hub.Id);
						dialogue.AddChild(hub);
						Add(hub);
					}
					
					if( childNode is Jump ) {
						Jump articyJump = (Jump)childNode;
						jump = new DialogueJump(articyJump.Id, articyJump.ExternalId);
						badScripts |= ImportArticyPins(articyJump, jump, true, true, pinIndex);
						GameDebugger.Log(LogLevel.Debug, "Adding jump '0x{0:X16}' to the dialogue", jump.Id);
						m_Objects.Add(jump.Id, jump);
						dialogue.AddChild(jump);
						Add(jump);
						articyJumps.Add(articyJump);
					}
				}
			}
			
			// jumps need postprocessing, because we must make sure all dialogue system nodes are already added
			BaseDialogueObject obj;
			DialoguePin pin;
			foreach( Jump articyJump in articyJumps ) {
				jump = (DialogueJump)m_Objects[articyJump.Id];

				GameDebugger.Log(LogLevel.Debug, "Linking jump '0x{0:X16}'", jump.Id);
				
				if( articyJump.Target != null ) {
					if( !m_Objects.TryGetValue(articyJump.Target.Id, out obj) )
						throw new UserFriendlyException(
							String.Format("Jump '0x{0:X16}' references an object outside the dialogue container", jump.Id),
							"One of data files is either corupt or has a version that is not supported by the game engine."
						);
					jump.Target = obj;
					GameDebugger.Log(LogLevel.Debug, "Linked target to {0} '0x{1:X16}'", obj.GetType().Name, obj.Id);
				}

				if( articyJump.TargetPin != null ) {
					if( articyJump.TargetPin.Node == dialogueContainer )
						throw new UserFriendlyException(
							String.Format("Jump '0x{0:X16}' references a pin of the dialogue container itself", jump.Id),
							"One of data files is either corupt or has a version that is not supported by the game engine."
						);
					if( !pinIndex.TryGetValue(articyJump.TargetPin.Id, out pin) )
						throw new UserFriendlyException(
							String.Format("Jump '0x{0:X16}' references a pin outside the dialogue container", jump.Id),
							"One of data files is either corupt or has a version that is not supported by the game engine."
						);
					jump.TargetPin = pin;
					GameDebugger.Log(LogLevel.Debug, "Linked target pin to '0x{0:X16}'", articyJump.TargetPin.Id);
				}
			}
			
			// recreate connections
			DialoguePin srcPin, dstPin;
			foreach( ArticyFlowConnection articyConnection in project.FlowConnections ) {
				if( articyConnection.Source.Node is ArticyFlowFragment
				   || articyConnection.Target.Node is ArticyFlowFragment
				   || (articyConnection.Source.Node is ArticyDialogue && articyConnection.Target.Node is ArticyDialogue)
				   || !pinIndex.TryGetValue(((FlowObjectPin)articyConnection.Source).Id, out srcPin)
				   || !pinIndex.TryGetValue(((FlowObjectPin)articyConnection.Target).Id, out dstPin)
				  )
					continue;

				GraphConnection newConnection = new GraphConnection();
				newConnection.Source = srcPin;
				newConnection.Target = dstPin;
				
				GameDebugger.Log(LogLevel.Debug, "Connected '0x{0:X16}' to '0x{1:X16}'", ((BaseDialogueObject)newConnection.Source.Node).Id, ((BaseDialogueObject)newConnection.Target.Node).Id);
			}
			
			return badScripts;
		}
	}
}
