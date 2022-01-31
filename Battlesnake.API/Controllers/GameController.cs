using Battlesnake.AI;
using Battlesnake.Algorithm;
using Battlesnake.API.Action;
using Battlesnake.DTOModel;
using Battlesnake.Enum;
using Battlesnake.Model;
using Battlesnake.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Battlesnake.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly ILogger<GameController> _logger;
        private readonly Dictionary<(string id, string name), Direction> _map;

        public GameController(ILogger<GameController> logger, Dictionary<(string id, string name), Direction> map)
        {
            _logger = logger;
            _map = map;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Whoami))]
        public IActionResult Get()
        {
            Whoami whoami = new("#33cc33", "V0.1");
            return Ok(whoami);
        }

        [HttpPost("Start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult PostStart(GameStatusDTO game)
        {
            string id = game.Game.Id;
            if (!_map.ContainsKey((id, game.You.Name))) _map.Add((id, game.You.Name), Direction.LEFT);
            //_logger.LogInformation($"{Util.LogPrefix(id)} New match has startet. {JsonConvert.SerializeObject(game)}");
            return Ok();
        }

        [HttpPost("Move")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MoveDTO))]
        public IActionResult PostMove(GameStatusDTO game)
        {
            Stopwatch watch = Stopwatch.StartNew();
            string id = game.Game.Id;
            if (!_map.ContainsKey((id, game.You.Name))) _map.Add((id, game.You.Name), Direction.LEFT);
            Direction currentDir = _map[(id, game.You.Name)];

            Algo algo = new(game, currentDir, watch);
            Direction newDir = algo.CalculateNextMove();
            _map[(id, game.You.Name)] = newDir;

            //_logger.LogInformation($"{Util.LogPrefix(id)} Previous direction: {currentDir} -- New direction: {newDir}");
            MoveDTO move = new() { Move = newDir.ToString().ToLower() };
            return Ok(move);
        }

        [HttpPost("End")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult PostEnd(GameStatusDTO game)
        {
            string id = game.Game.Id;
            if (_map.ContainsKey((id, game.You.Name))) _map.Remove((id, game.You.Name));
            //_logger.LogInformation($"{Util.LogPrefix(id)} Match ended");
            return Ok();
        }
    }
}
