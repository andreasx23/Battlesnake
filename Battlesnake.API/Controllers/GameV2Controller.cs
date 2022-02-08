using Battlesnake.Algorithm;
using Battlesnake.DTOModel;
using Battlesnake.Enum;
using Battlesnake.Model;
using Battlesnake.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Battlesnake.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameV2Controller : ControllerBase
    {
        private readonly ILogger<GameV2Controller> _logger;
        private readonly ConcurrentDictionary<(string gameId, string snakeId), Direction> _map;

        public GameV2Controller(ILogger<GameV2Controller> logger, ConcurrentDictionary<(string gameId, string snakeId), Direction> map)
        {
            _logger = logger;
            _map = map;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Whoami))]
        public IActionResult Get()
        {
            Whoami whoami = new(head: "scarf", tail: "present", colour: "#1a1a1a", version: "V1.3");
            return Ok(whoami);
        }

        [HttpPost("Start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult PostStart(GameStatusDTO game)
        {
            string id = game.Game.Id;
            string myId = game.You.Id;
            _map.TryAdd((id, myId), Direction.LEFT);
            return Ok();
        }

        [HttpPost("Move")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MoveDTO))]
        public IActionResult PostMove(GameStatusDTO game)
        {
            Stopwatch watch = Stopwatch.StartNew();
            string id = game.Game.Id;
            string myId = game.You.Id;
            if (Util.IsDebug) _map.TryAdd((id, myId), Direction.LEFT);

            Direction currentDir = _map[(id, myId)];
            Algo algo = new(game, currentDir, watch);
            Direction newDir = algo.CalculateNextMove();
            _map.TryUpdate((id, myId), newDir, currentDir);

            MoveDTO move = new() { Move = newDir.ToString().ToLower(), Shout = $"Took: {watch.Elapsed} to calculate the move" };
            return Ok(move);
        }

        [HttpPost("End")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult PostEnd(GameStatusDTO game)
        {
            string id = game.Game.Id;
            string myId = game.You.Id;
            _map.TryRemove((id, myId), out Direction value);
            return Ok();
        }
    }
}
