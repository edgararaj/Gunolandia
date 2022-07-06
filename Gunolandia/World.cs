using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace Gunolandia
{
    struct TileChunkPosition
    {
        public uint chunk_x;
        public uint chunk_y;
        public uint tile_x;
        public uint tile_y;
    };

    class WorldPosition
    {
        public uint abs_tile_x;
        public uint abs_tile_y;
        public float tile_rel_x;
        public float tile_rel_y;

        public Vector2 Distance(WorldPosition other)
        {
            return new Vector2(other.abs_tile_x - (int)abs_tile_x, other.abs_tile_y - (int)abs_tile_y);
        }

    }

    class Tile {
        public List<int> ids = new List<int>();
    }

    struct Chunk 
    { 
        public Tile[,] tiles;
    }

    class World
    {
        public const int chunk_shift = 4;
        public const int chunk_tile_dim = 1 << chunk_shift;
        public int chunk_dim = 256;
        public uint tile_size = 100;
        public Chunk[,] chunks;
        private Player relva = new Player(new Bitmap("assets/grass.png"), 1, 32, 32, Vector2.Zero);
        private Player arvore = new Player(new Bitmap("assets/arroz.png"), 1.4f, 66, 116, new Vector2(0, -50));
        private Player player = new Player();
        private WorldPosition player_position = new WorldPosition() { abs_tile_x = 50, abs_tile_y = 50, tile_rel_x = 0, tile_rel_y = 0 };
        private Random gerador = new Random();
        private WorldPosition camera_position;

        public World()
        {
            camera_position = player_position;
        }

        enum State { 
            Game, Edit
        }

        private State state = State.Game;

        public int ChunkMask {
            get { return (1 << chunk_shift) - 1;}
        }

        public int GetScale(int size, float a_tile_size)
        {
            return (int)(tile_size * a_tile_size / size);
        }

        public WorldPosition CenteredTilePoint(uint abs_x, uint abs_y)
        {
            WorldPosition resultado = new WorldPosition();
            resultado.abs_tile_x = abs_x;
            resultado.abs_tile_y = abs_y;

            return resultado;
        }

        private TileChunkPosition GetTileChunkPosition(uint abs_tile_x, uint abs_tile_y)
        {
            return new TileChunkPosition()
            {
                chunk_x = abs_tile_x >> chunk_shift,
                chunk_y = abs_tile_y >> chunk_shift,
                tile_x = (uint)(abs_tile_x & ChunkMask),
                tile_y = (uint)(abs_tile_y & ChunkMask)
            };
        }

        private Tile GetChunkTile(Chunk chunk, uint tile_x, uint tile_y)
        {
            if (chunk.tiles != null && tile_x < chunk_tile_dim && tile_y < chunk_tile_dim)
            {
                return chunk.tiles[tile_y, tile_x];
            }
            return null;
        }

        private Tile GetTile(uint abs_tile_x, uint abs_tile_y)
        {
            var chunk_pos = GetTileChunkPosition(abs_tile_x, abs_tile_y);
            if (chunk_pos.chunk_x < chunk_dim && chunk_pos.chunk_y < chunk_dim)
            {
                var chunk = chunks[chunk_pos.chunk_y, chunk_pos.chunk_x];
                return GetChunkTile(chunk, chunk_pos.tile_x, chunk_pos.tile_y);
            }
            return null;
        }

        public bool IsWorldTileEmpty(uint tile_x, uint tile_y)
        {
            var tile = GetTile(tile_x, tile_y);
            bool is_empty = true;
            if (tile != null)
            {
                foreach (var id in tile.ids)
                {
                    if (id != 0) is_empty = false;
                }
            }
            return is_empty;
        }

        public bool IsWorldPointEmpty(WorldPosition pos)
        {
            return IsWorldTileEmpty(pos.abs_tile_x, pos.abs_tile_y);
        }

        private WorldPosition TranslateWorldPosition(WorldPosition world_position, Vector2 delta)
        {
            WorldPosition result = new WorldPosition();
            var offset_x = world_position.tile_rel_x + delta.X;
            var offset_y = world_position.tile_rel_y + delta.Y;
            var tile_off_x = (int)Math.Round(offset_x / tile_size);
            var tile_off_y = (int)Math.Round(offset_y / tile_size);
            result.abs_tile_x = (uint)(world_position.abs_tile_x + tile_off_x);
            result.tile_rel_x = offset_x - tile_off_x * tile_size;
            result.abs_tile_y = (uint)(world_position.abs_tile_y + tile_off_y);
            result.tile_rel_y = offset_y - tile_off_y * tile_size;

            return result;
        }

        private WorldPosition GerarArvore(uint chunk_x, uint chunk_y)
        {
            WorldPosition resultado = new WorldPosition();
            int start_tile_x = 0;
            int start_tile_y = 0;
            if (chunk_x == 0)
            {
                start_tile_x = 1;
            }
            if (chunk_y == 0)
            {
                start_tile_y = 1;
            }
            resultado.abs_tile_x = (uint)(chunk_x * World.chunk_tile_dim + gerador.Next(start_tile_x, World.chunk_tile_dim));
            resultado.abs_tile_y = (uint)(chunk_y * World.chunk_tile_dim + gerador.Next(start_tile_y, World.chunk_tile_dim));

            return resultado;
        }

        public void Generate()
        { 
            chunks = new Chunk[chunk_dim, chunk_dim];
            for (uint chunk_y = 0; chunk_y < 8; chunk_y++)
            {
                for (uint chunk_x = 0; chunk_x < 8; chunk_x++)
                {
                    const int trees_per_chunk = 10;
                    var trees = new WorldPosition[trees_per_chunk];
                    for (uint i = 0; i < trees_per_chunk; i++)
                    {
                        WorldPosition new_tree = new WorldPosition();
                        var new_tree_is_valid = false;
                        while (new_tree_is_valid == false)
                        {
                            new_tree_is_valid = true;
                            new_tree = GerarArvore(chunk_x, chunk_y);
                            if (new_tree.Distance(player_position).Length() < 2)
                            { 
                                new_tree_is_valid = false;
                                continue;
                            }
                            foreach (var tree in trees)
                            {
                                if (tree != null)
                                {
                                    if (new_tree.Distance(tree).Length() < 4)
                                    {
                                        new_tree_is_valid = false;
                                        break;
                                    }
                                }
                            }
                        }
                        trees[i] = new_tree;
                    }

                    chunks[chunk_y, chunk_x].tiles = new Tile[World.chunk_tile_dim, World.chunk_tile_dim];
                    for (uint tile_x = 0; tile_x < World.chunk_tile_dim; tile_x++)
                    {
                        for (uint tile_y = 0; tile_y < World.chunk_tile_dim; tile_y++)
                        {
                            var abs_tile_x = tile_x + chunk_x * World.chunk_tile_dim;
                            var abs_tile_y = tile_y + chunk_y * World.chunk_tile_dim;
                            chunks[chunk_y, chunk_x].tiles[tile_y, tile_x] = new Tile();
                            foreach (var tree in trees)
                            {
                                if (abs_tile_x == tree.abs_tile_x && abs_tile_y == tree.abs_tile_y)
                                {
                                    chunks[chunk_y, chunk_x].tiles[tile_y, tile_x].ids.Add(2);
                                }
                            }

                            if (abs_tile_x == 0 || abs_tile_y == 0)
                                chunks[chunk_y, chunk_x].tiles[tile_y, tile_x].ids.Add(1);
                        }
                    }
                }
            }
        }

        public void Draw(Graphics canvas, int width, int height)
        {
            var center_x = width / 2;
            var center_y = height / 2;

            var tile_x_delta = (int)(width / 2 / tile_size) + 4;
            var tile_y_delta = (int)(height / 2 / tile_size) + 4;
            for (int rel_y = tile_y_delta; rel_y > -tile_y_delta; rel_y--)
            {
                var y = (uint)(camera_position.abs_tile_y + rel_y);
                for (int rel_x = -tile_x_delta; rel_x < tile_x_delta; rel_x++)
                {
                    var x = (uint)(camera_position.abs_tile_x + rel_x);
                    if (player_position.abs_tile_x == x && player_position.abs_tile_y == y)
                    {
                        var tile_diff_x = player_position.abs_tile_x - (int)camera_position.abs_tile_x;
                        var tile_diff_y = player_position.abs_tile_y - (int)camera_position.abs_tile_y;
                        var tile_diff_rel_x = player_position.tile_rel_x - camera_position.tile_rel_x;
                        var tile_diff_rel_y = player_position.tile_rel_y - camera_position.tile_rel_y;
                        var tile_center_x = (int)(center_x + tile_diff_x * tile_size + tile_diff_rel_x);
                        var tile_center_y = (int)(center_y - tile_diff_y * tile_size - tile_diff_rel_y);

                        var scale = GetScale(player.sprite_width, player.tile_width);
#if false
                        var display_collision_box = Utils.ScaleRect(player.collision_box, scale);
                        display_collision_box.Offset(tile_center_x, tile_center_y);
                        canvas.DrawRectangle(new Pen(Brushes.Blue), display_collision_box);
#endif

                        player.Draw(canvas, scale, tile_center_x, tile_center_y);
                    }

                    var a_tile_center_x = (int)(center_x + rel_x * tile_size - camera_position.tile_rel_x);
                    var tile_left = a_tile_center_x - tile_size / 2;
                    var a_tile_center_y = (int)(center_y - rel_y * tile_size + camera_position.tile_rel_y);
                    var tile_top = a_tile_center_y - tile_size / 2;
                    var tile = GetTile(y, x);
                    if (tile != null)
                    {
                        foreach (var id in tile.ids)
                        {

                            if (id == 1)
                            {
                                using (var brush = new SolidBrush(Color.FromArgb(255, 61, 61, 82)))
                                {
                                    canvas.FillRectangle(brush, tile_left, tile_top, tile_size, tile_size);
                                }
                            }
                            else if (id == 0)
                            {
                                var a_scale = GetScale(relva.sprite_width, relva.tile_width);
                                relva.Draw(canvas, a_scale, a_tile_center_x, a_tile_center_y);
                            }
                            else if (id == 2)
                            {
                                var a_scale = GetScale(arvore.sprite_width, arvore.tile_width);
                                arvore.Draw(canvas, a_scale, a_tile_center_x, a_tile_center_y);
                            }
                        }
                    }
#if false
                    using (var pen = new Pen(Brushes.Red))
                    {
                        canvas.DrawRectangle(pen, tile_left, tile_top, tile_size, tile_size);
                    }
#endif
                }
            }

            if (state == State.Edit)
            {
                canvas.FillRectangle(Brushes.Blue, 0, 0, 50, 50);
            }
        }

        public void KeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F)
            {
                player.wants_to_smoke = true;
            }
#if DEBUG
            else if (e.KeyCode == Keys.E)
            {
                state = state == State.Game ? State.Edit : State.Game;
            }
#endif
        }

        private uint GetTileByOffset(uint abs_tile, float offset)
        {
            var tile_off = (int)Math.Round(offset / tile_size);
            return (uint)(abs_tile + tile_off);
        }

        private Vector2 GetFloatDiff(WorldPosition pos, WorldPosition offset)
        {
            var result = new Vector2();

            result.X = (pos.abs_tile_x - offset.abs_tile_x) * tile_size + (pos.tile_rel_x - offset.tile_rel_x);
            result.Y = (pos.abs_tile_y - offset.abs_tile_y) * tile_size + (pos.tile_rel_y - offset.tile_rel_y);

            return result;
        }

        private void TestWall(float wall_x, float rel_x, float rel_y, float delta_x, float delta_y, ref float t_min, float min_y, float max_y)
        {
            var epsilon = 0.0001f;
            if (delta_x != 0)
            {
                float t_result = (wall_x - rel_x) / delta_x;
                float y = rel_y + t_result * delta_y;
                if (t_result >= 0 && t_min > t_result)
                {
                    if (y >= min_y && y <= max_y)
                    {
                        t_min = Math.Max(0f, t_result - epsilon);
                    }
                }
            }
        }

        public void Update(ulong delta_us)
        {
            var delta_sec = delta_us / 1e6f;

            Vector2 jogador_aceleracao = Vector2.Zero;
            Vector2 camera_velocity = Vector2.Zero;
            if (state == State.Game)
            {
                if ((Utils.KeyIsDown(Keys.W) || Utils.KeyIsDown(Keys.Up)))
                {
                    jogador_aceleracao.Y += 1;
                }
                if ((Utils.KeyIsDown(Keys.A) || Utils.KeyIsDown(Keys.Left)))
                {
                    jogador_aceleracao.X -= 1;
                }
                if ((Utils.KeyIsDown(Keys.S) || Utils.KeyIsDown(Keys.Down)))
                {
                    jogador_aceleracao.Y -= 1;
                }
                if ((Utils.KeyIsDown(Keys.D) || Utils.KeyIsDown(Keys.Right)))
                {
                    jogador_aceleracao.X += 1;
                }
            }
            else if (state == State.Edit)
            {
                if ((Utils.KeyIsDown(Keys.W) || Utils.KeyIsDown(Keys.Up)))
                {
                    camera_velocity.Y += 1;
                }
                if ((Utils.KeyIsDown(Keys.A) || Utils.KeyIsDown(Keys.Left)))
                {
                    camera_velocity.X -= 1;
                }
                if ((Utils.KeyIsDown(Keys.S) || Utils.KeyIsDown(Keys.Down)))
                {
                    camera_velocity.Y -= 1;
                }
                if ((Utils.KeyIsDown(Keys.D) || Utils.KeyIsDown(Keys.Right)))
                {
                    camera_velocity.X += 1;
                }
            }

            // Normalizar vetor
            jogador_aceleracao = Utils.Normalize(jogador_aceleracao);
            camera_velocity = Utils.Normalize(camera_velocity);

            jogador_aceleracao *= 20;

            jogador_aceleracao -= 10f * player.velocity;
            player.velocity += jogador_aceleracao * delta_sec;

            if (state == State.Edit)
            {
                camera_velocity *= 300 * delta_sec;
                camera_position = TranslateWorldPosition(camera_position, camera_velocity);
            }
            else {
                camera_position = player_position;
            }

            var new_player_position = TranslateWorldPosition(player_position, player.velocity);
            var old_player_position = player_position;
#if true
            var scale = GetScale(player.sprite_width, player.tile_width);
            var jogador_col_left = GetTileByOffset(new_player_position.abs_tile_x, new_player_position.tile_rel_x + player.collision_box.Left * scale);
            var jogador_col_top = GetTileByOffset(new_player_position.abs_tile_y, new_player_position.tile_rel_y - player.collision_box.Top * scale);
            var jogador_col_right = GetTileByOffset(new_player_position.abs_tile_x, new_player_position.tile_rel_x + player.collision_box.Right * scale);
            var jogador_col_bottom = GetTileByOffset(new_player_position.abs_tile_y, new_player_position.tile_rel_y - player.collision_box.Bottom * scale);
            bool not_collided_lt = IsWorldTileEmpty(jogador_col_top, jogador_col_left);
            bool not_collided_lb = IsWorldTileEmpty(jogador_col_bottom, jogador_col_left);
            bool not_collided_rt = IsWorldTileEmpty(jogador_col_top, jogador_col_right);
            bool not_collided_rb = IsWorldTileEmpty(jogador_col_bottom, jogador_col_right);
            if (not_collided_lb && not_collided_lt && not_collided_rb && not_collided_rt)
            {
                player_position = new_player_position;
            }
            else
            {
                Vector2 normal = Vector2.Zero;
                if (not_collided_lt && not_collided_rt)
                {
                    normal = new Vector2(0, -1);
                }
                if (not_collided_lb && not_collided_rb)
                {
                    normal = new Vector2(0, 1);
                }
                if (not_collided_lt && not_collided_lb)
                {
                    normal = new Vector2(1, 0);
                }
                if (not_collided_rt && not_collided_rb)
                {
                    normal = new Vector2(-1, 0);
                }
                player.velocity -= normal * Vector2.Dot(player.velocity, normal);
            }
#else
            var start_tile_x = old_player_position.abs_tile_x;
            var start_tile_y = old_player_position.abs_tile_y;
            var end_tile_x = new_player_position.abs_tile_x;
            var end_tile_y = new_player_position.abs_tile_y;

            var step_x = Math.Sign((int)end_tile_x - start_tile_x);
            var step_y = Math.Sign((int)end_tile_y - start_tile_y);

            if (false && Math.Abs(end_tile_x - start_tile_x) > 1)
                Debugger.Break();

            var t_min = 1f;
            var abs_tile_y = start_tile_y;
            while (true)
            {
                var abs_tile_x = start_tile_x;
                while (true)
                {
                    var test_tile = CenteredTilePoint(abs_tile_x, abs_tile_y);
                    if (!IsWorldPointEmpty(test_tile))
                    {
                        var tile = new Vector2(tile_size, tile_size);
                        var min_corner = -tile / 2f;
                        var max_corner = tile / 2f;
                        var rel_old_player = GetFloatDiff(old_player_position, test_tile);

                        TestWall(min_corner.X, rel_old_player.X, rel_old_player.Y, player.velocity.X, player.velocity.Y, ref t_min, min_corner.Y, max_corner.Y);
                        TestWall(max_corner.X, rel_old_player.X, rel_old_player.Y, player.velocity.X, player.velocity.Y, ref t_min, min_corner.Y, max_corner.Y);
                        TestWall(min_corner.Y, rel_old_player.Y, rel_old_player.X, player.velocity.Y, player.velocity.X, ref t_min, min_corner.X, max_corner.X);
                        TestWall(max_corner.Y, rel_old_player.Y, rel_old_player.X, player.velocity.Y, player.velocity.X, ref t_min, min_corner.X, max_corner.X);
                    }
                    if (abs_tile_x == end_tile_x)
                        break;
                    abs_tile_x = (uint)(abs_tile_x + step_x);
                }
                if (abs_tile_y == end_tile_y)
                    break;
                abs_tile_y = (uint)(abs_tile_y + step_y);
            }

            player.position = TranslateWorldPosition(player.position, player.velocity * t_min);
#endif
            player.IncrementTime(delta_us);
            player.rotate_flip = player.velocity.X < 0;
        }

        public void DrawLayer(Graphics canvas, int center_x, int center_y, Player player, int z, int width, int height)
        {
        }
    }
}
