using UnityEngine;
using System.Collections;

public class Character : MonoBehaviour 
{
	public MyTeam myTeam = MyTeam.Team1;

	public enum inputState 
	{ 
		None, 
		WalkLeft, 
		WalkRight, 
		Jump, 
		Pass
	}
	[HideInInspector] public inputState currentInputState;
	
	[HideInInspector] public enum facing { Right, Left }
	[HideInInspector] public facing facingDir;

	[HideInInspector] public bool alive = true;
	[HideInInspector] public Vector3 spawnPos;
	
	protected Transform _transform;
	protected Rigidbody2D _rigidbody;

	// edit these to tune character movement	
	private float runVel = 2.5f; 	// run speed when not carrying the ball
	private float walkVel = 2f; 	// walk speed while carrying ball
	private float jumpVel = 4f; 	// jump velocity
	private float jump2Vel = 2f; 	// double jump velocity
	private float fallVel = 1f;		// fall velocity, gravity
	private float passVel = 3f;		// horizontal velocity of ball when passed

	private float moveVel;
	private float pVel = 0f;
	
	private int jumps = 0;
    private int maxJumps = 2; 		// set to 2 for double jump
		
	protected bool hasBall = false;
	protected string team = "";

	// raycast stuff
	private RaycastHit2D hit;
	private Vector2 physVel = new Vector2();
	[HideInInspector] public bool grounded = false;
	private int groundMask = 1 << 8; // Ground layer mask

	public virtual void Awake()
	{
		_transform = transform;
		_rigidbody = rigidbody2D;
	}
	
	// Use this for initialization
	public virtual void Start () 
	{
		moveVel = walkVel;
	}
	
	// Update is called once per frame
	public virtual void UpdateMovement() 
	{
		if(xa.gameOver == true || alive == false) return;

		// if the other team took the ball from me, then remove it from my inventory
		if(myTeam == MyTeam.Team1 && xa.teamWithBall == xa.TeamWithBall.Team2)
		{
			RemoveBall();
		}

		if(myTeam == MyTeam.Team2 && xa.teamWithBall == xa.TeamWithBall.Team1)
		{
			RemoveBall();
		}

		// if I have then ball, then tell it to follow me
		if(hasBall == true)
		{
			xa.ball.UpdateBallFollowPos(_transform);
		}

		// teleport me to the other side of the screen when I reach the edge
		if(_transform.position.x > 4f)
		{
			_transform.position = new Vector3(-4f,_transform.position.y, 0);
		}
		if(_transform.position.x < -4f)
		{
			_transform.position = new Vector3(4f,_transform.position.y, 0);
		}
	}
	
	// ============================== FIXEDUPDATE ============================== 

	public virtual void UpdatePhysics()
	{
		if(xa.gameOver == true || alive == false) return;

		physVel = Vector2.zero;

		// move left
		if(currentInputState == inputState.WalkLeft)
		{
			physVel.x = -moveVel;
		}

		// move right
		if(currentInputState == inputState.WalkRight)
		{
			physVel.x = moveVel;
		}

		// jump
		if(currentInputState == inputState.Jump)
		{
			if(jumps < maxJumps)
			{
				jumps += 1;
				if(jumps == 1)
				{
					_rigidbody.velocity = new Vector2(physVel.x, jumpVel);
				}
				else if(jumps == 2)
				{
					_rigidbody.velocity = new Vector2(physVel.x, jump2Vel);
				}
			}
		}

		// pass the ball
		if(currentInputState == inputState.Pass && hasBall == true)// && _transform.childCount > 1)
		{
			if(facingDir == facing.Left)
				pVel = -passVel;
			else
				pVel = passVel;

			xa.ball.PassBall(pVel);
			RemoveBall();
		}

		// use raycasts to determine if the player is standing on the ground or not
		if (Physics2D.Raycast(new Vector2(_transform.position.x-0.1f,_transform.position.y), -Vector2.up, .26f, groundMask) 
		    || Physics2D.Raycast(new Vector2(_transform.position.x+0.1f,_transform.position.y), -Vector2.up, .26f, groundMask))
		{
			grounded = true;
			jumps = 0;
		}
		else
		{
			grounded = false;
			_rigidbody.AddForce(-Vector3.up * fallVel);
		}

		// actually move the player
		_rigidbody.velocity = new Vector2(physVel.x, _rigidbody.velocity.y);
	}

	// ============================== BALL HANDLING ==============================
	
	public virtual void PickUpBall()
	{
		hasBall = true;
		moveVel = walkVel;
		
		if(myTeam == MyTeam.Team1)
		{
			team = "Team1";
			xa.teamWithBall = xa.TeamWithBall.Team1;
		}
		else if(myTeam == MyTeam.Team2)
		{
			team = "Team2";
			xa.teamWithBall = xa.TeamWithBall.Team2;
		}
		
		xa.ball.PickUp(_transform, team);
		
		if(xa.scoreManager.gameType == GameType.Keepaway)
		{
			StartCoroutine(IncreaseScore());
		}
	}
	
	void RemoveBall()
	{
		hasBall = false;
		moveVel = runVel;
	}
	
	// if the ball gets stuck, player1 can reset it by pressing a key
	public virtual void ResetBall()
	{
		StartCoroutine(xa.ball.SpawnBall());
	}
	
	// ============================== KEEPAWAY SCORE ==============================
	
	IEnumerator IncreaseScore()
	{
		if(myTeam == MyTeam.Team1)
		{
			team = "Team1";
		}
		else
		{
			team = "Team2";
		}
		
		while(hasBall == true && xa.gameOver == false)
		{
			xa.scoreManager.IncreaseScore(team);
			yield return null;
		}
	}
}

public enum MyTeam 
{
	Team1,
	Team2,
	None
}
