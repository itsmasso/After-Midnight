using System.Collections;
using Pathfinding;
using UnityEngine;

public class LurkerRoamState : LurkerBaseState
{
    private FollowerEntity agent;
    private LurkerAnimationManager anim;
    public override void EnterState(LurkerMonsterScript lurker)
    {
        agent = lurker.agent;
        anim = lurker.animationManager;
        agent.maxSpeed = lurker.roamSpeed;
        lurker.currentTarget = null;
   
    }

    public override void UpdateState(LurkerMonsterScript lurker)
    {
		agent.stopDistance = lurker.defaultStoppingDistance;
    	agent.maxSpeed = lurker.roamSpeed;
    	agent.canMove = true;
    	
		SetPathfindingConstraints(true, 1 << 2);
		
		//Set animations
		anim.PlayWalkAnimation();
		
		//Search for random point to roam to
		if (!agent.pathPending && (agent.reachedEndOfPath || !agent.hasPath)) {
				NNInfo sample = AstarPath.active.graphs[0].RandomPointOnSurface(NNConstraint.Walkable);
				agent.destination = sample.position;
				agent.SearchPath();
		}
		
		TrySwitchToStalkState(lurker);
    }
    
    private void SetPathfindingConstraints(bool constrainTags, int tagMask)
    {
        if(AstarPath.active.GetNearest(agent.position).node.Tag != 1)
        {
            NNConstraint constraint = NNConstraint.Walkable;
			constraint.constrainTags = constrainTags;
			constraint.tags = tagMask; //only allow constraint to pick nodes with this tag
			agent.pathfindingSettings.traversableTags = tagMask; //only allow agent to move through nodes with this tag
        }
    }
    
    private void TrySwitchToStalkState(LurkerMonsterScript lurker)
    {
        //Random chance to switch to stalk state. checks every second
		float chance = Random.value;
		if(chance*100 <= lurker.chanceToStalkPlayer * Time.deltaTime && lurker.canStalk)
		{
			lurker.SetRandomPlayerAsTargetClientRpc();
			if(lurker.currentTarget != null)
			{
				
			    lurker.SwitchState(LurkerState.Stalking);
			}
			
		}
    }
    

	
}
