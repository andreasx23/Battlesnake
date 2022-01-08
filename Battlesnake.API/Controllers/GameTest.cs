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
        private readonly Dictionary<(string id, string name), Direction> _map;

        public GameTest(ILogger<GameTest> logger, Dictionary<(string id, string name), Direction> map)
        {
            _logger = logger;
            _map = map;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Whoami))]
        public IActionResult Get()
        {
            Whoami whoami = new(head: "mask", tail: "virus", colour: "#e580ff", version: "V1");
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
            if (Util.IsDebug && !_map.ContainsKey((id, game.You.Name))) _map.Add((id, game.You.Name), Direction.LEFT);

            Direction currentDir = _map[(id, game.You.Name)];
            Algo algo = new(game, currentDir, watch);
            Direction newDir = algo.CalculateNextMove(game.You);
            _map[(id, game.You.Name)] = newDir;

            //_logger.LogInformation($"{Util.LogPrefix(id)} -- Took: {watch.Elapsed} to calculate the move -- Previous direction: {currentDir} -- New direction: {newDir}");
            MoveDTO move = new() { Move = newDir.ToString().ToLower(), Shout = $"Took: {watch.Elapsed} to calculate the move" };
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
