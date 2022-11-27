using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WIAFN.AI
{
    public class SearchState : AIStateBase
    {
        public Vector3 lastSeenPosition;
        public Character searchTarget;

        public bool stopping;

        private float _updateSearchAtTime;

        private float _searchStartTime;

        private float _canSeeCheckTime;

        private const float canSeeCheckDeltaTime = 0.5f;
        public SearchState(Vector3 lastSeenPosition, Character searchTarget)
        {
            this.lastSeenPosition = lastSeenPosition;
            this.searchTarget = searchTarget;
            _updateSearchAtTime = Time.realtimeSinceStartup;
        }

        public void OnEnter(AIController ai)
        {
            stopping = false;
            ai.NPCController.SetTarget(lastSeenPosition);
            _searchStartTime = Time.realtimeSinceStartup;

            _canSeeCheckTime = 0f;
        }

        public void OnUpdate(AIController ai)
        {
            NPCController npc = ai.NPCController;
            _canSeeCheckTime += Time.deltaTime;
            if (_canSeeCheckTime > canSeeCheckDeltaTime)
            {
                _canSeeCheckTime = 0f;
                if (ai.AttackIfCanSeeCharacter(searchTarget)) { return; }
            }

            // Wait and look around.
            if (!stopping && ai.IsStopped)
            {
                npc.Stop();
                stopping = true;
                _updateSearchAtTime = Time.realtimeSinceStartup + Random.Range(0f, 3f);
            }

            // Continue searching.
            if (stopping && Time.realtimeSinceStartup > _updateSearchAtTime)
            {
                npc.SetTarget(lastSeenPosition + (Random.insideUnitSphere * ai.maxPatrolDistance));
                stopping = false;
            }

            if (Time.realtimeSinceStartup - _searchStartTime > ai.searchDuration)
            {
                ai.ChangeState(new IdleState());
            }
        }

        public void OnExit(AIController ai)
        {
            ai.NPCController.Stop();
        }
    }
}