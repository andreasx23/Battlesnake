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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Battlesnake.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameTest : ControllerBase
    {
        private readonly ILogger<GameTest> _logger;
        private readonly Dictionary<string, Direction> _map;

        public GameTest(ILogger<GameTest> logger, Dictionary<string, Direction> map)
        {
            _logger = logger;
            _map = map;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Whoami))]
        public IActionResult Get()
        {
            Whoami whoami = new(head: "scarf", tail: "present", colour: "#1a1a1a", version: "V1");
            return Ok(whoami);
        }

        [HttpPost("Start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult PostStart(GameStatusDTO game)
        {
            string id = game.Game.Id;
            if (!_map.ContainsKey(id)) _map.Add(id, Direction.LEFT);
            _logger.LogInformation($"{Util.LogPrefix(id)} New match has startet. {JsonConvert.SerializeObject(game)}");
            return Ok();
        }

        [HttpPost("Move")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MoveDTO))]
        public IActionResult PostMove(GameStatusDTO game)
        {
            Stopwatch watch = Stopwatch.StartNew();
            string id = game.Game.Id;
            if (!_map.ContainsKey(id)) _map.Add(id, Direction.LEFT);

            Direction currentDir = _map[id];
            Algo algo = new(game, currentDir, watch);
            Direction newDir = algo.CalculateNextMove(game.You, true);
            _map[id] = newDir;

            _logger.LogInformation($"{Util.LogPrefix(id)} -- Took: {watch.Elapsed} to calculate the move -- Previous direction: {currentDir} -- New direction: {newDir}");
            MoveDTO move = new() { Move = newDir.ToString().ToLower(), Shout = $"Took: {watch.Elapsed} to calculate the move" };
            return Ok(move);
        }

        [HttpPost("End")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult PostEnd(GameStatusDTO game)
        {
            string id = game.Game.Id;
            if (_map.ContainsKey(id)) _map.Remove(id);
            _logger.LogInformation($"{Util.LogPrefix(id)} Match ended");
            return Ok();
        }
    }
}
