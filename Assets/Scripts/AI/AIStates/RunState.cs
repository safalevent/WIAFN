using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WIAFN.AI
{
    public class RunState : ConditionalAIStateBase
    {
        public Character fromCharacter;

        private const float runDistance = 30f;
        private const float runDirectionRandomize = 30f;
        public RunState(Character fromCharacter)
        {
            this.fromCharacter = fromCharacter;
        }

        public void OnEnter(AIController ai)
        {
            NPCController npc = ai.NPCController;
            SetRunTargetPos(ai);
        }

        public void OnUpdate(AIController ai)
        {
            if (!ai.CanFeelCharacter(fromCharacter))
            {
                ai.ChangeState(new IdleState());
            }

            if (ai.IsStopped)
            {
                SetRunTargetPos(ai);
            }
        }

        public void OnExit(AIController ai)
        {

        }

        private void SetRunTargetPos(AIController ai)
        {
            NPCController npc = ai.NPCController;
            Vector3 runDirection = (npc.transform.position - fromCharacter.transform.position).normalized;
            runDirection = Quaternion.Euler(runDirectionRandomize, runDirectionRandomize, runDirectionRandomize) * runDirection * runDistance;

            npc.SetRelativeTarget(runDirection);
        }

        public static bool CheckCondition(AIController ai)
        {
            NPCController npc = ai.NPCController;
            return npc.character.health < npc.character.BaseStats.maxHealth / 4f;
        }
    }
}