using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WIAFN.AI
{
    public class IdleState : AIStateBase
    {
        private float _chosenIdleTime;
        private float _passedTime;

        private float _canSeeCheckTime;

        private const float canSeeCheckDeltaTime = 0.1f;

        public AudioManager audioManager;

        private void Awake()
        {
            //audioManager = Object.FindObjectOfType<AudioManager>();
            audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();

        }

        public void OnEnter(AIController ai)
        {
            NPCControllerBase npc = ai.NPCController;
            npc.StopMoving();
            _passedTime = 0f;
            _canSeeCheckTime = 0f;
            _chosenIdleTime = Random.Range(0f, ai.maxIdleTime);

            var _baseStats = ai.GetComponent<CharacterBaseStats>();
            _baseStats.speedCoefficient = 7000f;

            AudioManager.instance.PlayEnemyRandom(ai.transform);
        }

        public void UpdateState(AIController ai)
        {
            _passedTime += Time.deltaTime;
            _canSeeCheckTime += Time.deltaTime;

            if (_canSeeCheckTime > canSeeCheckDeltaTime)
            {
                _canSeeCheckTime = 0f;
                if (ai.SwitchToAttackIfCanSeePlayer())
                {
                    return;
                }
            }

            if (_passedTime >= _chosenIdleTime)
            {
                ai.ChangeState(new PatrolState());
            }

        }

        public void UpdateNPCBehaviour(AIController ai)
        {
            
        }

        public void OnExit(AIController ai)
        {
        }
    }
}
